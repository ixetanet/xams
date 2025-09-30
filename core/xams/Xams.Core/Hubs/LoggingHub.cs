// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;


namespace Xams.Core.Hubs;

[ServiceHub("Logging")]
public class LoggingHub : IServiceHub
{
    private static readonly string GroupName = "Xams_Logging_Group";
    private static readonly ConcurrentDictionary<string, ReceiveMessage> ConnectionFilters = new();
    public Task<Response<object?>> OnConnected(HubContext context)
    {
        context.Groups.AddToGroupAsync(context.SignalRContext.ConnectionId, GroupName);
        return Task.FromResult(ServiceResult.Success());
    }

    public Task<Response<object?>> OnDisconnected(HubContext context)
    {
        context.Groups.RemoveFromGroupAsync(context.SignalRContext.ConnectionId, GroupName);
        ConnectionFilters.TryRemove(context.SignalRContext.ConnectionId, out _);
        return Task.FromResult(ServiceResult.Success());
    }

    public async Task<Response<object?>> OnReceive(HubContext context)
    {
        var message = context.GetMessage<ReceiveMessage>();
        if (message.Type == "initial_load")
        {
            // Store the filters for this connection
            ConnectionFilters.AddOrUpdate(context.SignalRContext.ConnectionId, message, (_, _) => message);
            
            var db = context.GetDbContext<IXamsDbContext>();
            var query = db.Logs.AsQueryable();

            // Filter by log levels
            if (message.Levels != null && message.Levels.Any())
            {
                query = query.Where(log => message.Levels.Contains(log.Level));
            }

            // Filter by search text in message or properties
            if (!string.IsNullOrWhiteSpace(message.Search))
            {
                query = query.Where(log => log.Message.Contains(message.Search) || (log.Properties != null && log.Properties.Contains(message.Search)));
            }

            // Filter by user ID (exact match on UserId field)
            if (!string.IsNullOrWhiteSpace(message.UserId) && Guid.TryParse(message.UserId, out var userId))
            {
                query = query.Where(log => log.UserId == userId);
            }

            // Filter by job history ID (exact match on JobHistoryId field)
            if (!string.IsNullOrWhiteSpace(message.JobHistoryId) && Guid.TryParse(message.JobHistoryId, out var jobHistoryId))
            {
                query = query.Where(log => log.JobHistoryId == jobHistoryId);
            }

            // Filter by date range
            if (message.StartDate.HasValue)
            {
                var startDateUtc = DateTime.SpecifyKind(message.StartDate.Value, DateTimeKind.Utc);
                query = query.Where(log => log.Timestamp >= startDateUtc);
            }
            if (message.EndDate.HasValue)
            {
                var endDateUtc = DateTime.SpecifyKind(message.EndDate.Value, DateTimeKind.Utc);
                query = query.Where(log => log.Timestamp <= endDateUtc);
            }

            var dbLogs = await query
                .OrderByDescending(log => log.Timestamp)
                .Take(100) // Limit to last 100 logs
                .Select(log => new { log.Timestamp, log.Message, log.Properties, log.Level, log.LogId, log.UserId, log.JobHistoryId, log.Exception, log.Environment, log.RequestPath, log.ClientIp })
                .ToListAsync();

            var logs = dbLogs.Select(log => 
            {
                var properties = !string.IsNullOrEmpty(log.Properties) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(log.Properties) ?? new Dictionary<string, object>()
                    : new Dictionary<string, object>();
                
                // Add the core fields to properties
                if (!string.IsNullOrWhiteSpace(log.Environment))
                    properties["Environment"] = log.Environment;
                if (!string.IsNullOrWhiteSpace(log.RequestPath))
                    properties["RequestPath"] = log.RequestPath;
                if (log.UserId.HasValue)
                    properties["UserId"] = log.UserId.Value.ToString();
                if (log.JobHistoryId.HasValue)
                    properties["JobHistoryId"] = log.JobHistoryId.Value.ToString();
                if (!string.IsNullOrWhiteSpace(log.ClientIp))
                    properties["ClientIp"] = log.ClientIp;
                
                return new SendMessage
                {
                    LogId = log.LogId,
                    Message = log.Message,
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    UserId = log.UserId,
                    JobHistoryId = log.JobHistoryId,
                    Exception = log.Exception,
                    Properties = properties
                };
            }).ToArray();
            
            return ServiceResult.Success(logs);
        }
        // Return error, clients should not be sending messages to this hub
        return ServiceResult.Success();
    }
    
    public class ReceiveMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string>? Levels { get; set; }
        public string? Search { get; set; }
        public string? UserId { get; set; }
        public string? JobHistoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public Task<Response<object?>> Send(HubSendContext context)
    {
        var logMessage = context.Message as SendMessage;
        if (logMessage == null)
        {
            return Task.FromResult(ServiceResult.Success());
        }
        
        foreach (var (connectionId, filters) in ConnectionFilters)
        {
            if (LogMatchesFilters(logMessage, filters))
            {
                context.Clients.Client(connectionId).SendAsync("Log", logMessage);
            }
        }
        
        return Task.FromResult(ServiceResult.Success());
    }

    private static bool LogMatchesFilters(SendMessage log, ReceiveMessage filters)
    {
        // Check log levels
        if (filters.Levels != null && filters.Levels.Any())
        {
            if (!filters.Levels.Contains(log.Level))
                return false;
        }

        // Check search text in message or properties
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            // Serialize Properties to JSON string to match how it's stored and searched in the database
            var propertiesJson = JsonSerializer.Serialize(log.Properties);
            if (!log.Message.Contains(filters.Search) && !propertiesJson.Contains(filters.Search))
                return false;
        }

        // Check user ID (exact match on UserId field)
        if (!string.IsNullOrWhiteSpace(filters.UserId) && Guid.TryParse(filters.UserId, out var userId))
        {
            if (log.UserId != userId)
                return false;
        }

        // Check job history ID (exact match on JobHistoryId field)
        if (!string.IsNullOrWhiteSpace(filters.JobHistoryId) && Guid.TryParse(filters.JobHistoryId, out var jobHistoryId))
        {
            if (log.JobHistoryId != jobHistoryId)
                return false;
        }

        // Check date range
        if (filters.StartDate.HasValue && log.Timestamp < filters.StartDate.Value)
            return false;
        
        if (filters.EndDate.HasValue && log.Timestamp > filters.EndDate.Value)
            return false;

        return true;
    }
    
    public class SendMessage
    {
        public Guid LogId { get; set; }
        public string? Exception { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string? SourceContext { get; set; }
        public Guid? UserId { get; set; }
        public Guid? JobHistoryId { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}