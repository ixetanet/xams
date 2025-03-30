using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Permission;

[ServiceJob(nameof(PermissionCacheJob), "permission-cache", "00:00:05", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class PermissionCacheJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        // Retrieve all the security cache records since the last retrieval date
        var db = context.GetDbContext<IXamsDbContext>();
        var systemMetadata = Cache.Instance.GetTableMetadata("System");
        var dLinq = new DynamicLinq(db, systemMetadata.Type);
        var query = dLinq.Query.Where("Name == @0 && DateTime > @1", "SECURITY_CACHE", PermissionCache.LastUpdateTime);
        var refreshDateTime = DateTime.UtcNow;
        var secrurityQueue = await query.ToDynamicListAsync();
        
        foreach (var item in secrurityQueue)
        {
            string[] values = item.Value.Split(",");
            var operation = values[0];
            var tableName = values[1];
            var id = Guid.Empty;
            if (values.Length > 2)
            {
                Guid.TryParse(values[2], out id); ;    
            }
            string oldPermissionName = string.Empty;
            string newPermissionName = string.Empty;
            if (tableName == "Permission")
            {
                oldPermissionName = values[2];
                newPermissionName = values[3];
            }
            
            
            if (operation == "Delete")
            {
                if (tableName == "Role")
                {
                    context.Logger.LogInformation("Role deleted");
                    PermissionCache.RemoveRole(id);
                }

                if (tableName == "User")
                {
                    context.Logger.LogInformation("User deleted");
                    PermissionCache.RemoveUser(id);
                }

                if (tableName == "Team")
                {
                    context.Logger.LogInformation("Team deleted");
                    PermissionCache.RemoveTeam(id);
                }
            }

            if (tableName == "RolePermission")
            {
                context.Logger.LogInformation("RolePermission refresh");
                await PermissionCache.CacheRolePermissions(db, id);
            }

            if (tableName == "UserRole")
            {
                context.Logger.LogInformation("UserRole refresh");
                await PermissionCache.CacheUserRoles(db, id);
            }

            if (tableName == "TeamUser")
            {
                context.Logger.LogInformation("TeamUser refresh");
                await PermissionCache.CacheUserTeams(db, id);
            }

            if (tableName == "TeamRole")
            {
                context.Logger.LogInformation("TeamRole refresh");
                await PermissionCache.CacheTeamRoles(db, id);
            }

            if (tableName == "Permission")
            {
                if (operation == "Update")
                {
                    context.Logger.LogInformation("Permission updated");
                    PermissionCache.UpdatePermission(oldPermissionName, newPermissionName);
                }

                if (operation == "Delete")
                {
                    context.Logger.LogInformation("Permission deleted");
                    PermissionCache.RemovePermission(values[1]);
                }
            }
        }
        
        PermissionCache.LastUpdateTime = refreshDateTime;

        try
        {
            query = dLinq.Query.Where("Name == @0 && DateTime < @1", "SECURITY_CACHE", DateTime.UtcNow.AddMinutes(-5));
            secrurityQueue = await query.ToDynamicListAsync();
            var saveChanges = false;
            foreach (var item in secrurityQueue)
            {
                // Delete old security cache records
                db.Remove(item);
                saveChanges = true;
            }
            
            if (saveChanges)
            {
                await db.SaveChangesAsync();
                context.Logger.LogInformation("Removed old cache records");
            }
        }
        catch (Exception e)
        {
            context.Logger.LogWarning(e, "Failed to remove old security cache records in System table, possibility of multiple servers deleting at the same time.");
        }
        
        return ServiceResult.Success();
    }
}