using System.Linq.Dynamic.Core;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using Timer = System.Timers.Timer;

namespace Xams.Core.Jobs;

public class JobService
{
    public static JobService? Singleton { get; set; }

    public int PingInterval { get; set; } =
        5; // Seconds, while a job is running, the job record is updated with the current time
    public string? DefaultServerName { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly List<JobQueue> _jobQueues = new();
    private readonly Timer _timer; 
    private bool _isRunning;
    
    public JobService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Initialize Job Queues
        using (var scope = _serviceProvider.CreateScope())
        {
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            // Get all jobs
            BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("Job");

            DynamicLinq<BaseDbContext> dynamicLinq =
                new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            var jobs = query.ToDynamicList();
            var queues = jobs
                .Where(x => !string.IsNullOrEmpty((string?)x.Queue))
                .Select(x => (string)x.Queue).Distinct();
            foreach (var queue in queues)
            {
                _jobQueues.Add(new JobQueue(_serviceProvider)
                {
                    Name = queue
                });
            }
        }
        // Maintain a handle to the timer to prevent it from being garbage collected
        _timer = new Timer(TimeSpan.FromSeconds(1));
        _timer.Elapsed += ExecuteQueues;
        _timer.Start();
    }

    internal static void Initialize(IServiceProvider serviceProvider)
    {
        Singleton = new JobService(serviceProvider);
    }

    private async void ExecuteQueues(object? sender, ElapsedEventArgs args)
    {
        if (_isRunning)
        {
            return;
        }

        try
        {
            _isRunning = true;
            using (var scope = _serviceProvider.CreateScope())
            {
                await ExecuteManualJobs(scope);
                await ExecuteJobs(scope);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error executing Job Queues: {e.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }
    
    /// <summary>
    /// Jobs that have been triggered by calling the ADMIN_TriggerJob action
    /// </summary>
    /// <param name="scope"></param>
    private async Task ExecuteManualJobs(IServiceScope scope)
    {
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        // Get all jobs
        BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
        
        var systemDLinq = new DynamicLinq<BaseDbContext>(baseDbContext, Cache.Instance.GetTableMetadata("System").Type);
        var systemQuery = systemDLinq.Query;
        systemQuery = systemQuery
            .Where("Name == @0", $"EXECUTE_JOB_{Cache.Instance.ServerName}")
            .Where("DateTime > @0", DateTime.UtcNow.AddSeconds(-30));
        var systemRecords = await systemQuery.ToDynamicListAsync();
        
        // Delete system records to prevent re-triggering
        foreach (var systemRecord in systemRecords)
        {
            baseDbContext.Remove(systemRecord);
        }
        await baseDbContext.SaveChangesAsync();

        var jobMetadata = Cache.Instance.GetTableMetadata("Job");
        foreach (var systemRecord in systemRecords)
        {
            Console.WriteLine($"Running on Server: {Cache.Instance.ServerName}");
            DynamicLinq<BaseDbContext> dLinqJobs =
                new DynamicLinq<BaseDbContext>(baseDbContext, jobMetadata.Type);
            IQueryable query = dLinqJobs.Query;
            string jobName = systemRecord.Value;
            query = query.Where("Name == @0", jobName);
            // Get all jobs every execution because the job might have been activated\deactivated
            var jobRecord = query.ToDynamicList().FirstOrDefault();
            if (jobRecord == null)
            {
                continue;
            }
            var job = new Job(jobRecord);
            await job.Execute(scope, true);
        }
        
        // Delete stale records
        baseDbContext.ChangeTracker.Clear();
        systemQuery = systemDLinq.Query;
        systemQuery = systemQuery
            .Where("Name.StartsWith(@0)", $"EXECUTE_JOB_")
            .Where("DateTime < @0", DateTime.UtcNow.AddSeconds(-30));
        systemRecords = await systemQuery.ToDynamicListAsync();
        foreach (var systemRecord in systemRecords)
        { 
            baseDbContext.Remove(systemRecord);
        }
        await baseDbContext.SaveChangesAsync();
        
    }

    /// <summary>
    /// Trigger jobs on a schedule
    /// </summary>
    /// <param name="scope"></param>
    private async Task ExecuteJobs(IServiceScope scope)
    {
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        // Get all jobs
        BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
        var jobMetadata = Cache.Instance.GetTableMetadata("Job");
        // Execute all scheduled jobs
        DynamicLinq<BaseDbContext> dLinqJobs =
            new DynamicLinq<BaseDbContext>(baseDbContext, jobMetadata.Type);
        IQueryable query = dLinqJobs.Query;
        // Get all jobs every execution because the job might have been actived\deactivated
        var dynamicJobs = query.ToDynamicList();
        var jobs = dynamicJobs.Select(x => new Job(x)).ToList();
                
        // Get the default server to execute jobs that should only run on 1 server, where a specific
        // server hasn't been specified
        DynamicLinq<BaseDbContext> dlinqServer =
            new DynamicLinq<BaseDbContext>(baseDbContext, Cache.Instance.GetTableMetadata("Server").Type);
        IQueryable serverQuery = dlinqServer.Query;
        serverQuery = serverQuery.OrderBy("Name asc, LastPing desc");
        // Get all jobs every execution because the job might have been updated
        var defaultServer = serverQuery.Take(1).ToDynamicList().FirstOrDefault();
        DefaultServerName = defaultServer?.Name;

        await Parallel.ForEachAsync(_jobQueues, async (jobQueue, _) =>
        {
            var jobsInQueue = jobs.Where(x => x.Queue == jobQueue.Name).ToList();
            await jobQueue.Execute(jobsInQueue);
        });
    }
}



