using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core.Jobs;

public class Job
{
    public Guid JobId 
    {
        get => Entity.JobId;
        set => Entity.JobId = value;
    }
    public string Name
    {
        get => Entity.Name;
        set => Entity.Name = value;
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
    public string? Tag 
    {
        get => Entity.Tag;
        set => Entity.Tag = value!;
    }
    
    private Dictionary<string, JsonElement> Parameters { get; } 
    
    public dynamic Entity { get; }
    
    public Cache.ServiceJobInfo ServiceJobInfo { get; set; }
    
    private Guid? JobHistoryId { get; }

    public Job(dynamic jobObject, Dictionary<string, JsonElement> parameters, Guid? jobHistoryId = null)
    {
        Entity = jobObject;
        ServiceJobInfo = Cache.Instance.ServiceJobs[jobObject.Name];
        Parameters = parameters;
        JobHistoryId = jobHistoryId;
    }

    public async Task<Response<object?>> Execute(IServiceScope? scope, bool force = false)
    {
        if (scope == null)
        {
            throw new Exception($"Service Scope is null");
        }
        
        // If this job is only supposed to execute on 1 server
        // but a specific server hasn't been specified, execute on the default server only
        if (Cache.Instance.ServiceJobs[Name].ExecuteJobOn == ExecuteJobOn.One &&
            string.IsNullOrEmpty(Cache.Instance.ServiceJobs[Name].ServerName) &&
            JobService.Singleton?.DefaultServerName != Cache.Instance.ServerName)
        {
            return ServiceResult.Success();
        }
        
        // If this job is only supposed to execute on 1 server
        // and a specific server was specified, only execute the job on that server
        if (Cache.Instance.ServiceJobs[Name].ExecuteJobOn == ExecuteJobOn.One &&
            !string.IsNullOrEmpty(Cache.Instance.ServiceJobs[Name].ServerName) &&
            Cache.Instance.ServiceJobs[Name].ServerName != Cache.Instance.ServerName)
        {
            return ServiceResult.Success();
        }
        
        var serviceJob = ServiceJobInfo.Type;

        if (serviceJob == null)
        {
            throw new Exception($"Couldn't find job type for {Name}");
        }
        
        var serviceJobInstance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, serviceJob);
        MethodInfo? methodInfo = serviceJob.GetMethod("Execute");

        if (methodInfo == null)
        {
            throw new Exception($"Could not find Execute method on job {Name}");
        }
        
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        
        IXamsDbContext xamsDbContext = dataService.GetDataRepository().CreateNewDbContext();
        
        Guid jobHistoryId = JobHistoryId ?? Guid.NewGuid();
        Type jobHistoryType = Cache.Instance.GetTableMetadata("JobHistory").Type;
        JobPing? jobPing = null;
        
        // Push JobHistoryId to Serilog context so all logs within this job execution have JobHistoryId
        using var logContext = LogContext.PushProperty("JobHistoryId", jobHistoryId);
        
        try
        {
            // Get the last Job history record for this server
            DynamicLinq dlinqJobHistory = new DynamicLinq(xamsDbContext, jobHistoryType);
            IQueryable jobHistoryQuery = dlinqJobHistory.Query;
            jobHistoryQuery = jobHistoryQuery
                .Where("JobId == @0", JobId)
                .Where("ServerName == @0", Cache.Instance.ServerName).OrderBy("Ping DESC").Take(1);
            dynamic? jobHistory = (await jobHistoryQuery.ToDynamicListAsync()).FirstOrDefault();
            
            // If the job is running, but the last ping was three times the ping interval, then assume the job failed
            if (jobHistory != null && jobHistory?.Status == "Running" && DateTime.UtcNow - jobHistory?.Ping >
                TimeSpan.FromSeconds(JobService.Singleton!.PingInterval * 3))
            {
                xamsDbContext.ChangeTracker.Clear();
                
                // Update Job History Records
                string jobStatus = "Failed";
                xamsDbContext.ChangeTracker.Clear();
                DynamicLinq dynamicLinq =
                    new DynamicLinq(xamsDbContext, jobHistoryType);
                IQueryable query = dynamicLinq.Query;
                query = query.Where($"JobId == @0", JobId);
                query = query.Where($"Status == @0", "Running");
                List<dynamic> jobHistories = await query.ToDynamicListAsync();
                foreach (var jHistory in jobHistories)
                {
                    jHistory.Status = jobStatus;
                    jHistory.CompletedDate = DateTime.UtcNow;
                    xamsDbContext.Update(jHistory);
                }
                await xamsDbContext.SaveChangesAsync();
            }

            string jobName = Name ?? throw new Exception($"Job missing name.");

            // If this job has been deleted, skip
            if (!Cache.Instance.ServiceJobs.ContainsKey(jobName))
            {
                return ServiceResult.Success();
            }

            // If the job is running, potentially on another server, skip
            if (jobHistory?.Status == "Running")
            {
                return ServiceResult.Success();
            }
            
            // If the job isn't active skip it
            if (!IsActive && !force)
            {
                return ServiceResult.Success();
            }

            DateTime jobLastExecution =  jobHistory?.CreatedDate ?? DateTime.MinValue; 
            
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
                // If this job has no job history
                var executeTime = DateTime.UtcNow.Date.Add(jobInfo.TimeSpan);
                var daylightSavingsTimeZone = Cache.Instance.ServiceJobs[jobName].TimeZone;
                if (!string.IsNullOrEmpty(daylightSavingsTimeZone))
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(daylightSavingsTimeZone);
                    var todayInTargetTimezone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
                    var localDateTime = todayInTargetTimezone.Add(jobInfo.TimeSpan);
                    executeTime = localDateTime.ToUniversalTime(); 
                }
                
                if (!(DateTime.UtcNow >= executeTime &&
                      DateTime.UtcNow - executeTime < TimeSpan.FromMinutes(1) &&
                      DateTime.UtcNow - jobLastExecution > TimeSpan.FromMinutes(1)))
                {
                    return ServiceResult.Success();
                }
            }

            // Create Job History Record
            string status = "Running";
            xamsDbContext.ChangeTracker.Clear();
            jobHistory = Activator.CreateInstance(jobHistoryType)!;
            jobHistory.JobHistoryId = jobHistoryId;
            jobHistory.Status = status;
            jobHistory.CreatedDate = DateTime.UtcNow;
            jobHistory.JobId = JobId;
            jobHistory.Name = Name;
            jobHistory.Message = string.Empty;
            jobHistory.ServerName = Cache.Instance.ServerName;
            xamsDbContext.Add(jobHistory);
            await xamsDbContext.SaveChangesAsync();
                
                
            // Update job to running
            dynamic? updateJob = await xamsDbContext.FindAsync(Entity.GetType(), JobId);
            xamsDbContext.ChangeTracker.Clear();
            try
            {
                // It's possible multiple servers try to update the LastExecution at the same time
                // and create a contention issue. In that case, ignore and move on.
                if (updateJob == null)
                {
                    throw new Exception($"Couldn't find Job to update with Id {JobId}");
                }
                updateJob.LastExecution = DateTime.UtcNow;
                xamsDbContext.Update(updateJob);
                await xamsDbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                // ignored
            }


            // Start updating the Ping field on the job on a set interval to show this job is running
            jobPing = new(dataService.GetDataRepository().CreateNewDbContext(), jobHistory);
            jobPing.Start();

            // Execute job
            var jobDataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            
            var pipelineContext = new PipelineContext()
            {
                UserId = SystemRecords.SystemUserId,
                InputParameters = Parameters,
                OutputParameters = new Dictionary<string, JsonElement>(),
                SystemParameters = new SystemParameters(),
                DataService = jobDataService,
                DataRepository = jobDataService.GetDataRepository(),
                MetadataRepository = jobDataService.GetMetadataRepository(),
                SecurityRepository = jobDataService.GetSecurityRepository(),
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
            xamsDbContext.ChangeTracker.Clear();
            jobHistory = await xamsDbContext.FindAsync(jobHistoryType, jobHistoryId) ?? 
                         throw new Exception($"Failed to find Job History Id {jobHistoryId}");
            jobHistory.Status = status;
            jobHistory.Message = jobResponse.FriendlyMessage ?? string.Empty;
            jobHistory.CompletedDate = DateTime.UtcNow;
            xamsDbContext.Update(jobHistory);
            await xamsDbContext.SaveChangesAsync();

            // Update job last execution
            xamsDbContext.ChangeTracker.Clear();
            try
            {
                // It's possible multiple servers try to update the LastExecution at the same time
                // and create a contention issue. In that case, ignore and move on.
                updateJob = await xamsDbContext.FindAsync(Entity.GetType(), JobId);
                if (updateJob == null)
                {
                    throw new Exception($"Couldn't find Job to update last execution with Id {JobId}");
                }
                // updateJob.Status = status;
                updateJob.LastExecution = DateTime.UtcNow;
                xamsDbContext.Update(updateJob);
                await xamsDbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                // ignored
            }


            return jobResponse;
        }
        catch (Exception e)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();
            logger.LogError(e, e.Message);
            try
            {
                // Stop pinging the server since the job failed
                if (jobPing != null)
                {
                    await jobPing.End();    
                }
                
                // Update job history
                xamsDbContext.ChangeTracker.Clear();
                string status = "Failed";
                dynamic jobHistory = await xamsDbContext.FindAsync(jobHistoryType, jobHistoryId) ??
                                     throw new Exception($"Failed to find Job History Id {jobHistoryId}");
                if (jobHistory != null)
                {
                    jobHistory.Status = status;
                    jobHistory.Message = e.StackTrace ?? e.InnerException?.ToString() ?? string.Empty;
                    jobHistory.CompletedDate = DateTime.UtcNow;
                    xamsDbContext.Update(jobHistory);
                    await xamsDbContext.SaveChangesAsync();    
                }
                    
                // Update job last execution
                xamsDbContext.ChangeTracker.Clear();
                dynamic? updateJob = await xamsDbContext.FindAsync(Entity.GetType(), JobId);
                if (updateJob == null)
                {
                    throw new Exception($"Multiple errors attempting to update Job with Id {JobId}");
                }
                if (updateJob != null)
                {
                    // updateJob.Status = status;
                    updateJob.LastExecution = DateTime.UtcNow;
                    xamsDbContext.Update(updateJob);
                    await xamsDbContext.SaveChangesAsync();    
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
