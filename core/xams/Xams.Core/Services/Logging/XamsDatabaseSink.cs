using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using Xams.Core.Entities;
using Xams.Core.Hubs;
using Xams.Core.Interfaces;

namespace Xams.Core.Services.Logging;

public class XamsDatabaseSink : ILogEventSink, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<LogItem> _queue = new();
    private readonly Timer _timer;
    private readonly int _batchSize = 100;
    private readonly int _flushIntervalMs = 2000;
    private readonly string _applicationName;
    private readonly string _version;
    private volatile bool _disposed;
    
    public XamsDatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
        _version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
        _timer = new Timer(FlushBatch, null, _flushIntervalMs, _flushIntervalMs);
    }
    
    public void Emit(LogEvent logEvent)
    {
        if (_disposed) return;

        var logItem = new LogItem()
        {
            LogEvent = logEvent,
            LogId = Guid.NewGuid()
        };
        
        _queue.Enqueue(logItem);
        
        // Force flush if queue is getting large
        if (_queue.Count >= _batchSize * 2)
        {
            _ = Task.Run(() => FlushBatch(null));
        }
        
        // Send to SignalR hub
        var userId = logEvent.Properties.ContainsKey("UserId") && 
                     Guid.TryParse(logEvent.Properties["UserId"].ToString().Trim('"'), out var uid) ? uid : (Guid?)null;
        var jobHistoryId = logEvent.Properties.ContainsKey("JobHistoryId") && 
                           Guid.TryParse(logEvent.Properties["JobHistoryId"].ToString().Trim('"'), out var jhid) ? jhid : (Guid?)null;
        
        var properties = logEvent.Properties.ToDictionary(
            kvp => kvp.Key,
            kvp => (object)kvp.Value.ToString().Trim('"')
        );
        
        // Add core fields to properties if they exist
        if (logEvent.Properties.ContainsKey("EnvironmentName"))
            properties["Environment"] = logEvent.Properties["EnvironmentName"].ToString().Trim('"');
        if (logEvent.Properties.ContainsKey("RequestPath"))
            properties["RequestPath"] = logEvent.Properties["RequestPath"].ToString().Trim('"');
        if (userId.HasValue)
            properties["UserId"] = userId.Value.ToString();
        if (jobHistoryId.HasValue)
            properties["JobHistoryId"] = jobHistoryId.Value.ToString();
        if (logEvent.Properties.ContainsKey("ClientIp"))
            properties["ClientIp"] = logEvent.Properties["ClientIp"].ToString().Trim('"');
        
        var logData = new LoggingHub.SendMessage()
        {
            LogId = logItem.LogId,
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            SourceContext = logEvent.Properties.GetValueOrDefault("SourceContext")?.ToString(),
            Exception = logEvent.Exception?.ToString(),
            UserId = userId,
            JobHistoryId = jobHistoryId,
            Properties = properties
        };

        _ = Task.Run(async () =>
        {
            var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            await dataService.HubSend<LoggingHub>(logData);
        });
    }
    
    private async void FlushBatch(object? state)
    {
        if (_disposed) return;
        
        var logs = new List<Log>();
        var count = 0;
        
        while (count < _batchSize && _queue.TryDequeue(out var logEvent))
        {
            try
            {
                logs.Add(ConvertToLog(logEvent));
                count++;
            }
            catch (Exception ex)
            {
                // Log conversion failed - continue with other logs
                System.Diagnostics.Debug.WriteLine($"Failed to convert log event: {ex.Message}");
            }
        }
        
        if (logs.Any())
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                using var xamsDbContext = dataService.GetDataRepository().CreateNewDbContext();
                
                // Cast to access Logs DbSet
                if (xamsDbContext is DbContext dbContext)
                {
                    await dbContext.Set<Log>().AddRangeAsync(logs);
                    await xamsDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Database write failed - logs are lost but we don't want to crash the app
                System.Diagnostics.Debug.WriteLine($"Failed to write logs to database: {ex.Message}");
            }
        }
    }
    
    private Log ConvertToLog(LogItem logItem)
    {
        var log = new Log
        {
            LogId = logItem.LogId,
            Timestamp = logItem.LogEvent.Timestamp.UtcDateTime,
            Level = logItem.LogEvent.Level.ToString(),
            Message = logItem.LogEvent.RenderMessage(),
            MessageTemplate = logItem.LogEvent.MessageTemplate.Text,
            Exception = logItem.LogEvent.Exception?.ToString(),
            ApplicationName = _applicationName,
            Version = _version
        };
        
        // Extract known properties
        var extraProperties = new Dictionary<string, object>();
        
        foreach (var prop in logItem.LogEvent.Properties)
        {
            var value = ExtractPropertyValue(prop.Value);
            
            switch (prop.Key)
            {
                case "SourceContext":
                    log.SourceContext = value?.ToString();
                    break;
                case "MachineName":
                    log.MachineName = value?.ToString();
                    break;
                case "EnvironmentName":
                    log.Environment = value?.ToString();
                    break;
                case "ThreadId":
                    if (int.TryParse(value?.ToString(), out var threadId))
                        log.ThreadId = threadId;
                    break;
                case "UserId":
                    if (Guid.TryParse(value?.ToString(), out var userId))
                        log.UserId = userId;
                    break;
                case "JobHistoryId":
                    if (Guid.TryParse(value?.ToString(), out var jobHistoryId))
                        log.JobHistoryId = jobHistoryId;
                    break;
                case "UserName":
                    log.UserName = value?.ToString();
                    break;
                case "CorrelationId":
                    if (Guid.TryParse(value?.ToString(), out var correlationId))
                        log.CorrelationId = correlationId;
                    break;
                case "RequestId":
                    log.RequestId = value?.ToString();
                    break;
                case "RequestPath":
                    log.RequestPath = value?.ToString();
                    break;
                case "RequestMethod":
                    log.RequestMethod = value?.ToString();
                    break;
                case "StatusCode":
                    if (int.TryParse(value?.ToString(), out var statusCode))
                        log.StatusCode = statusCode;
                    break;
                case "ElapsedMs":
                case "Elapsed":
                    if (double.TryParse(value?.ToString(), out var elapsed))
                        log.ElapsedMs = elapsed;
                    break;
                case "ClientIp":
                    log.ClientIp = value?.ToString();
                    break;
                case "UserAgent":
                    log.UserAgent = value?.ToString();
                    break;
                default:
                    if (value != null)
                        extraProperties[prop.Key] = value;
                    break;
            }
        }
        
        // Handle exception details
        if (logItem.LogEvent.Exception != null)
        {
            log.ExceptionType = logItem.LogEvent.Exception.GetType().Name;
            log.ExceptionMessage = logItem.LogEvent.Exception.Message;
        }
        
        // Serialize extra properties as JSON
        if (extraProperties.Any())
        {
            try
            {
                log.Properties = JsonSerializer.Serialize(extraProperties);
            }
            catch (Exception)
            {
                // JSON serialization failed - skip extra properties
            }
        }
        
        return log;
    }
    
    private static object? ExtractPropertyValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(ExtractPropertyValue).ToArray(),
            StructureValue structure => structure.Properties.ToDictionary(
                kvp => kvp.Name,
                kvp => ExtractPropertyValue(kvp.Value)
            ),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                kvp => ExtractPropertyValue(kvp.Key)?.ToString() ?? "",
                kvp => ExtractPropertyValue(kvp.Value)
            ),
            _ => propertyValue.ToString().Trim('"')
        };
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _timer?.Dispose();
        
        // Flush remaining logs
        FlushBatch(null);
    }

    public class LogItem
    {
        public Guid LogId { get; set; }
        public required LogEvent LogEvent { get; set; }
    }
    
}