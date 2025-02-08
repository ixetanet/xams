using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Servers;

/// <summary>
/// Ping the database every 5 seconds
/// </summary>
///
[JobServer(ExecuteJobOn.All)]
[ServiceJob(nameof(ServerJob), "System-Servers", "00:00:05", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class ServerJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        // Get a list of all servers ordered by serverid
        var db = context.GetDbContext<BaseDbContext>();
        Type serverType = Cache.Instance.GetTableMetadata("Server").Type;
        
        DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, serverType);
        var query = dynamicLinq.Query.OrderBy("Name asc, LastPing desc");
        var results = await query.ToDynamicArrayAsync();
        
        // If this server is not in the last then add it, otherwise update the ping
        var server = results.FirstOrDefault(x => x.ServerId == Cache.Instance.ServerId);
        if (server == null)
        {
            var serverDict = new Dictionary<string, dynamic>();
            serverDict["ServerId"] = Cache.Instance.ServerId;
            serverDict["Name"] = Cache.Instance.ServerName ?? "Unknown";
            serverDict["LastPing"] = DateTime.UtcNow;
            db.Add(EntityUtil.DictionaryToEntity(serverType, serverDict));
            await db.SaveChangesAsync();
        }
        else
        {
            server.LastPing = DateTime.UtcNow;
            db.Update(server);
            await db.SaveChangesAsync();
        }
        
        // If the first server in the list is this server that has a ping less than 30,
        // then continue otherwise exit (only have 1 server managing the list of servers)
        dynamic? firstServer = null;
        foreach (var entity in results)
        {
            if (((DateTime)entity.LastPing).AddSeconds(30) > DateTime.UtcNow)
            {
                firstServer = entity;
                break;
            }
        }
        
        if (firstServer == null || firstServer?.ServerId != Cache.Instance.ServerId)
        {
            return ServiceResult.Success();
        }
        
        // For any servers that haven't called home in 30 seconds, delete
        foreach (var entity in results)
        {
            if (((DateTime)entity.LastPing).AddSeconds(30) < DateTime.UtcNow)
            {
                db.Remove(entity);
            }
        }
        await db.SaveChangesAsync();
        
        
        return ServiceResult.Success();
    }
}