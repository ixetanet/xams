using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core.Services.Jobs;

public class Job
{
    public Guid JobId 
    {
        get => Entity.JobId;
        set => Entity.JobId = value;
    }
    public string? Name
    {
        get => Entity.Name;
        set => Entity.Name = value!;
    }

    public bool IsActive 
    {
        get => Entity.IsActive;
        set => Entity.IsActive = value;
    }
    public string? Queue 
    {
        get => Entity.Queue;
        set => Entity.Queue = value!;
    }
    public string? Status 
    {
        get => Entity.Status;
        set => Entity.Status = value!;
    }
    public DateTime LastExecution 
    {
        get => Entity.LastExecution;
        set => Entity.LastExecution = value;
    }
    public DateTime Ping 
    {
        get => Entity.Ping;
        set => Entity.Ping = value;
    }
    public string? Tag 
    {
        get => Entity.Tag;
        set => Entity.Tag = value!;
    }
    
    public dynamic Entity { get; }
    
    public Cache.ServiceJobInfo ServiceJobInfo { get; set; }

    public Job(dynamic jobObject)
    {
        Entity = jobObject;
        ServiceJobInfo = Cache.Instance.ServiceJobs[jobObject.Name];
    }

    public async Task<Response<object?>> Execute(IServiceScope? scope, bool force = false)
    {
        if (scope == null)
        {
            throw new Exception($"Service Scope is null");
        }
        
        var serviceJob = ServiceJobInfo.Type;

        if (serviceJob == null)
        {
            throw new Exception($"Couldn't find job type for {Name}");
        }
                            
        var serviceJobInstance = Activator.CreateInstance(serviceJob);
        MethodInfo? methodInfo = serviceJob.GetMethod("Execute");

        if (methodInfo == null)
        {
            throw new Exception($"Could not find Execute method on job {Name}");
        }
        
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

        // Get all jobs
        BaseDbContext baseDbContext = dataService.GetDataRepository().CreateNewDbContext();
        
        Guid jobHistoryId = Guid.NewGuid();
        Type jobHistoryType = Cache.Instance.GetTableMetadata("JobHistory").Type;
        JobPing jobPing = new(dataService.GetDataRepository().CreateNewDbContext(), Entity);
        try
        {
            // If the job is running, but the last ping was three times the ping interval, then assume the job failed
            if (Status == "Running" && DateTime.UtcNow - Ping >
                TimeSpan.FromSeconds(JobService.Singleton!.PingInterval * 3))
            {
                baseDbContext.ChangeTracker.Clear();
                string jobStatus = "Failed";
                dynamic? uJob = await baseDbContext.FindAsync(Entity.GetType(), JobId);
                if (uJob == null)
                {
                    throw new Exception($"Couldn't find Job with Id {JobId}");
                }
                uJob.Status = jobStatus;
                baseDbContext.Update(uJob);
                await baseDbContext.SaveChangesAsync();
                    
                // Update Job History Records
                baseDbContext.ChangeTracker.Clear();
                DynamicLinq<BaseDbContext> dynamicLinq =
                    new DynamicLinq<BaseDbContext>(baseDbContext, jobHistoryType);
                IQueryable query = dynamicLinq.Query;
                query = query.Where($"JobId == @0", JobId);
                query = query.Where($"Status == @0", "Running");
                List<dynamic> jobHistories = await query.ToDynamicListAsync();
                foreach (var jHistory in jobHistories)
                {
                    jHistory.Status = jobStatus;
                    jHistory.CompletedDate = DateTime.UtcNow;
                    baseDbContext.Update(jHistory);
                }
                await baseDbContext.SaveChangesAsync();
            }

            string jobName = Name ?? throw new Exception($"Job missing name.");

            // If this job has been deleted, skip
            if (!Cache.Instance.ServiceJobs.ContainsKey(jobName))
            {
                return ServiceResult.Success();
            }

            // If the job is running, potentially on another server, skip
            if (Status == "Running")
            {
                return ServiceResult.Success();
            }
            
            // If the job isn't active skip it
            if (!IsActive && !force)
            {
                return ServiceResult.Success();
            }

            DateTime jobLastExecution = LastExecution;
            var jobInfo = Cache.Instance.ServiceJobs[jobName].ServiceJobAttribute;

            bool isDayOfWeek =
                jobInfo.DaysOfWeek.HasFlag(ConvertDayOfWeek(DateTime.UtcNow.DayOfWeek));

            if (!isDayOfWeek && !force)
            {
                return ServiceResult.Success();
            }

            // If this is running on an interval, and the last execution is less than the interval, skip
            if (!force && jobInfo.JobSchedule == JobSchedule.Interval &&
                DateTime.UtcNow - jobLastExecution < jobInfo.TimeSpan)
            {
                return ServiceResult.Success();
            }

            // If this is running for a specific time, and it's within a minute of the scheduled time
            // And the last execution is more than a minute ago, then execute
            if (!force && jobInfo.JobSchedule == JobSchedule.TimeOfDay)
            {
                var executeTime = DateTime.UtcNow.Date.Add(jobInfo.TimeSpan);
                if (!(DateTime.UtcNow >= executeTime &&
                      DateTime.UtcNow - executeTime < TimeSpan.FromMinutes(1) &&
                      DateTime.UtcNow - jobLastExecution > TimeSpan.FromMinutes(1)))
                {
                    return ServiceResult.Success();
                }
            }

            // Create Job History Record
            string status = "Running";
            baseDbContext.ChangeTracker.Clear();
            dynamic jobHistory = Activator.CreateInstance(jobHistoryType)!;
            jobHistory.JobHistoryId = jobHistoryId;
            jobHistory.Status = status;
            jobHistory.CreatedDate = DateTime.UtcNow;
            jobHistory.JobId = JobId;
            jobHistory.Name = Name;
            jobHistory.Message = string.Empty;
            baseDbContext.Add(jobHistory);
            await baseDbContext.SaveChangesAsync();
                
                
            // Update job to running
            baseDbContext.ChangeTracker.Clear();
            dynamic? updateJob = await baseDbContext.FindAsync(Entity.GetType(), JobId);
            if (updateJob == null)
            {
                throw new Exception($"Couldn't find Job to update with Id {JobId}");
            }
            updateJob.Status = status;
            updateJob.LastExecution = DateTime.UtcNow;
            baseDbContext.Update(updateJob);
            await baseDbContext.SaveChangesAsync();
                

            // Start updating the Ping field on the job on a set interval to show this job is running
            jobPing.Start();

            // Execute job
            var jobDataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            
            var pipelineContext = new PipelineContext()
            {
                UserId = SystemRecords.SystemUserId,
                OutputParameters = new Dictionary<string, JsonElement>(),
                SystemParameters = new SystemParameters(),
                DataService = jobDataService,
                DataRepository = jobDataService.GetDataRepository(),
                MetadataRepository = jobDataService.GetMetadataRepository(),
                SecurityRepository = jobDataService.GetSecurityRepository()
            };
            
            Response<object?> jobResponse = await ((Task<Response<object?>>)methodInfo.Invoke(serviceJobInstance,
            [
                new JobServiceContext(pipelineContext)
            ])!);
                                
            // If there were any create, update or delete operations on the job,
            // then call bulk service to execute the operations
            await jobDataService.TryExecuteBulkServiceLogic(BulkStage.Post, SystemRecords.SystemUserId);

            // Stop job ping
            await jobPing.End();
                
            // Update job history
            status = jobResponse.Succeeded ? "Completed" : "Failed";
            baseDbContext.ChangeTracker.Clear();
            jobHistory = await baseDbContext.FindAsync(jobHistoryType, jobHistoryId) ?? 
                         throw new Exception($"Failed to find Job History Id {jobHistoryId}");
            jobHistory.Status = status;
            jobHistory.Message = jobResponse.FriendlyMessage ?? string.Empty;
            jobHistory.CompletedDate = DateTime.UtcNow;
            baseDbContext.Update(jobHistory);
            await baseDbContext.SaveChangesAsync();

            // Update job last execution
            baseDbContext.ChangeTracker.Clear();
            updateJob = await baseDbContext.FindAsync(Entity.GetType(), JobId);
            if (updateJob == null)
            {
                throw new Exception($"Couldn't find Job to update last execution with Id {JobId}");
            }
            updateJob.Status = status;
            updateJob.LastExecution = DateTime.UtcNow;
            baseDbContext.Update(updateJob);
            await baseDbContext.SaveChangesAsync();

            return jobResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error executing job {Name}: {e.Message}");
            try
            {
                // Stop pinging the server since the job failed
                await jobPing.End();
                // Update job history
                baseDbContext.ChangeTracker.Clear();
                string status = "Failed";
                dynamic jobHistory = await baseDbContext.FindAsync(jobHistoryType, jobHistoryId) ??
                                     throw new Exception($"Failed to find Job History Id {jobHistoryId}");
                if (jobHistory != null)
                {
                    jobHistory.Status = status;
                    jobHistory.Message = e.InnerException?.ToString() ?? string.Empty;
                    jobHistory.CompletedDate = DateTime.UtcNow;
                    baseDbContext.Update(jobHistory);
                    await baseDbContext.SaveChangesAsync();    
                }
                    
                // Update job last execution
                baseDbContext.ChangeTracker.Clear();
                dynamic? updateJob = await baseDbContext.FindAsync(Entity.GetType(), JobId);
                if (updateJob == null)
                {
                    throw new Exception($"Multiple errors attempting to update Job with Id {JobId}");
                }
                if (updateJob != null)
                {
                    updateJob.Status = status;
                    updateJob.LastExecution = DateTime.UtcNow;
                    baseDbContext.Update(updateJob);
                    await baseDbContext.SaveChangesAsync();    
                }

                return ServiceResult.Success();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to update job history: {exception.Message}");
                return ServiceResult.Error($"Failed to update job history: {exception.Message}");
            }
                
        }
    }
    
    public static DaysOfWeek ConvertDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => DaysOfWeek.Sunday,
            DayOfWeek.Monday => DaysOfWeek.Monday,
            DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
            DayOfWeek.Thursday => DaysOfWeek.Thursday,
            DayOfWeek.Friday => DaysOfWeek.Friday,
            DayOfWeek.Saturday => DaysOfWeek.Saturday,
            _ => DaysOfWeek.None
        };
    }
}