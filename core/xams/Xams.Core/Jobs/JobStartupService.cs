using System.Linq.Dynamic.Core;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Jobs;

// Set order to 100 to ensure startup services that jobs are dependent on are executed first
[ServiceStartup(StartupOperation.Post, 100)]
public class JobStartupService : IServiceStartup
{
    public static string SettingName = "JOB_HISTORY_RETENTION_DAYS";
    private IDataService? _dataService;

    public async Task<Response<object?>> Execute(StartupContext startupContext)
    {
        _dataService = startupContext.ServiceProvider.GetRequiredService<IDataService>();
        await CreateJobs();
        
        // This starts execution of the jobs
        // Recreate the scope and service provider to ensure the service provider is not disposed
        JobService.Initialize(startupContext.ServiceProvider.CreateScope().ServiceProvider);

        return ServiceResult.Success();
    }

    // Ensure jobs exist in database
    public async Task CreateJobs()
    {
        if (_dataService == null)
        {
            Console.WriteLine("Failed to create jobs, data service is null");
            return;
        }
        Console.WriteLine("Creating Jobs");
        IXamsDbContext xamsDbContext = _dataService.GetDataRepository().CreateNewDbContext();
        var baseDbContextType = Cache.Instance.GetTableMetadata("Job");

        DynamicLinq dynamicLinq = new DynamicLinq(xamsDbContext, baseDbContextType.Type);
        IQueryable query = dynamicLinq.Query;

        // Get all jobs
        var jobs = await query.ToDynamicListAsync();
        
        // Delete any jobs that no longer exist
        List<dynamic> removedJobs = new List<dynamic>();
        foreach (var job in jobs)
        {
            var jobName = (string)job.Name;
            var actualJobExists = Cache.Instance.ServiceJobs.Any(m => m.Value.ServiceJobAttribute.Name == jobName);
            if (actualJobExists == false)
            {
                xamsDbContext.Remove(job);
                removedJobs.Add(job);
            }
        }
        
        foreach (var job in removedJobs)
        {
            jobs.Remove(job);
        }

        try
        {
            await xamsDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error creating jobs: {e.Message}");
        }
        finally
        {
            xamsDbContext.ChangeTracker.Clear();
        }

        // Get all job ids
        var jobIds = jobs.Select(j => (Guid)j.JobId).ToList();
        
        foreach (var jobInfo in Cache.Instance.ServiceJobs)
        {
            Guid jobId = GuidUtil.FromString(jobInfo.Value.ServiceJobAttribute.Name);
            if (!jobIds.Contains(jobId))
            {
                Dictionary<string, dynamic> job = new();
                job["JobId"] = jobId;
                job["Queue"] = jobInfo.Value.ServiceJobAttribute.Queue;
                job["Name"] = jobInfo.Value.ServiceJobAttribute.Name;
                job["IsActive"] = jobInfo.Value.State == JobState.Active;
                job["Status"] = "Waiting";
                job["Tag"] = jobInfo.Value.ServiceJobAttribute.Tag;
                job["LastExecution"] = DateTime.MinValue.ToUniversalTime();
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, job);
                xamsDbContext.Add(entity);
            }
            // If the name of the queue has changed, update it
            else if (jobInfo.Value.ServiceJobAttribute.Queue != jobs.First(j => (Guid)j.JobId == jobId).Queue)
            {
                var job = jobs.First(j => (Guid)j.JobId == jobId);
                job.Queue = jobInfo.Value.ServiceJobAttribute.Queue;
                xamsDbContext.Update(job);
            }
        }
        
        await xamsDbContext.SaveChangesAsync();

        
    }

}