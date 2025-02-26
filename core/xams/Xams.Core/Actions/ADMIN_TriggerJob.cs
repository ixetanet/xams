using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Jobs;
using Xams.Core.Utils;

namespace Xams.Core.Actions;

[ServiceAction("ADMIN_TriggerJob")]
public class ADMIN_TriggerJob : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (context.Parameters == null)
        {
            return ServiceResult.Error($"Missing parameters");
        }

        if (!context.Parameters.ContainsKey("jobName"))
        {
            return ServiceResult.Error($"Missing parameter jobName");
        }
        
        string? jobName = context.Parameters["jobName"].GetString();

        if (string.IsNullOrEmpty(jobName))
        {
            return ServiceResult.Error($"jobName cannot be null or empty string");
        }
        
        // Create system records to execute job(s)
        if (!Cache.Instance.ServiceJobs.ContainsKey(jobName))
        {
            return ServiceResult.Error($"Job {jobName} not found");
        }
        
        var job = Cache.Instance.ServiceJobs[jobName];
        var db = context.GetDbContext<BaseDbContext>();

        // Execute on the first server alphabetically
        if (job.ExecuteJobOn is ExecuteJobOn.One && string.IsNullOrEmpty(job.ServerName))
        {
            DynamicLinq<BaseDbContext> dLinq =
                new DynamicLinq<BaseDbContext>(db, Cache.Instance.GetTableMetadata("Server").Type);
            IQueryable query = dLinq.Query;
            query = query.Take(1).OrderBy("Name asc").Where("LastPing > @0", DateTime.UtcNow.AddSeconds(-30));
            var server = (await query.ToDynamicListAsync()).FirstOrDefault();
            if (server == null)
            {
                return ServiceResult.Error($"Server {job.ServerName} not found");
            }

            var system = new Dictionary<string, dynamic>();
            system["Name"] = $"EXECUTE_JOB_{server.Name}";
            system["Value"] = jobName;
            system["DateTime"] = DateTime.UtcNow;
            db.Add(EntityUtil.DictionaryToEntity(Cache.Instance.GetTableMetadata("System").Type, system));
        }
        // Execute on a specific server
        else if (job.ExecuteJobOn is ExecuteJobOn.One && !string.IsNullOrEmpty(job.ServerName))
        {
            DynamicLinq<BaseDbContext> dLinq =
                new DynamicLinq<BaseDbContext>(db, Cache.Instance.GetTableMetadata("Server").Type);
            IQueryable query = dLinq.Query;
            query = query.Take(1).OrderBy("Name asc")
                .Where("LastPing > @0", DateTime.UtcNow.AddSeconds(-30))
                .Where("Name == @0", job.ServerName);
            var server = (await query.ToDynamicListAsync()).FirstOrDefault();
            if (server == null)
            {
                return ServiceResult.Error($"Server {job.ServerName} not found");
            }
            
            var system = new Dictionary<string, dynamic>();
            system["Name"] = $"EXECUTE_JOB_{server.Name}";
            system["Value"] = jobName;
            system["DateTime"] = DateTime.UtcNow;
            db.Add(EntityUtil.DictionaryToEntity(Cache.Instance.GetTableMetadata("System").Type, system));
        }
        // Execute on all servers
        else
        {
            DynamicLinq<BaseDbContext> dLinq =
                new DynamicLinq<BaseDbContext>(db, Cache.Instance.GetTableMetadata("Server").Type);
            IQueryable query = dLinq.Query;
            query = query.Where("LastPing > @0", DateTime.UtcNow.AddSeconds(-30));
            var servers = await query.ToDynamicListAsync();
            var serversDistinct = servers.Select(x => x.Name).Distinct();

            foreach (var serverName in serversDistinct)
            {
                var system = new Dictionary<string, dynamic>();
                system["Name"] = $"EXECUTE_JOB_{serverName}";
                system["Value"] = jobName;
                system["DateTime"] = DateTime.UtcNow;
                db.Add(EntityUtil.DictionaryToEntity(Cache.Instance.GetTableMetadata("System").Type, system));    
            }
        }
        
        await db.SaveChangesAsync();

        return ServiceResult.Success();
    }
}