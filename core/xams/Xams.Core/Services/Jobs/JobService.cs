using System.Linq.Dynamic.Core;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using Timer = System.Timers.Timer;

namespace Xams.Core.Services.Jobs;

public class JobService
{
    public static JobService? Singleton { get; set; }

    public int PingInterval { get; set; } =
        5; // Seconds, while a job is running, the job record is updated with the current time

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

    // Force execute job (used in Admin Dashboard)
    public async Task ExecuteJob(string jobName)
    {
        if (!Cache.Instance.ServiceJobs.ContainsKey(jobName))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        // Retrieve job
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
        var baseDbContextType = Cache.Instance.GetTableMetadata("Job");
        DynamicLinq<BaseDbContext> dynamicLinq =
            new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
        IQueryable query = dynamicLinq.Query;
        query = query.Where("Name = @0", jobName);
        query = query.Where("Status != @0", "Running");
        List<dynamic> dynamicJobs = await query.ToDynamicListAsync();
        List<Job> jobs = dynamicJobs.Select(x => new Job(x)).ToList();

        Job? job = jobs.FirstOrDefault();

        if (job == null)
        {
            return;
        }

        await job.Execute(scope, true);
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
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                // Get all jobs
                BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
                var baseDbContextType = Cache.Instance.GetTableMetadata("Job");

                DynamicLinq<BaseDbContext> dynamicLinq =
                    new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
                IQueryable query = dynamicLinq.Query;
                // Get all jobs every execution because the job might have been updated
                var dynamicJobs = query.ToDynamicList();
                var jobs = dynamicJobs.Select(x => new Job(x)).ToList();

                await Parallel.ForEachAsync(_jobQueues, async (jobQueue, _) =>
                {
                    var jobsInQueue = jobs.Where(x => x.Queue == jobQueue.Name).ToList();
                    await jobQueue.Execute(jobsInQueue);
                });
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
}



