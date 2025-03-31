using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;

namespace Xams.Core.Services.Permission;

/// <summary>
/// Whenever the permissions are updated, this service will invalidate the cache so all running instances will refresh their cache.
/// </summary>
[BulkService(BulkStage.Post)]
public class PermissionCacheService : IBulkService
{
    private static readonly string[] SecurityEntities = ["Permission", "Role", "RolePermission", "UserRole", "TeamUser", "TeamRole", "User", "Team"];
    
    public async Task<Response<object?>> Execute(BulkServiceContext context)
    {
        if (context.AllServiceContexts.All(x => x.DataOperation is Read))
        {
            return ServiceResult.Success();
        }
        
        if (!context.AllServiceContexts.Any(x => SecurityEntities.Contains(x.TableName)))
        {
            return ServiceResult.Success();
        }
        
        var db = context.GetDbContext<IXamsDbContext>();
        db.ChangeTracker.Clear();

        // Only track\communicate changes that will affect permission assignments
        bool saveChanges = HandlePermission(context) ||
                           HandleDelete(context, "Role", "Id") || // Handle role deletes
                           HandleDelete(context, "User", "Id") || // Handle user deletes
                           HandleDelete(context, "Team", "TeamId") || // Handle team deletes
                           HandleEntity(context, "RolePermission", "RoleId") || // Refresh all permissions for this role
                           HandleEntity(context, "UserRole", "UserId") || // Refresh all roles for this user
                           HandleEntity(context, "TeamUser", "UserId") || // Refresh all teams for this user
                           HandleEntity(context, "TeamRole", "TeamId"); // Refresh all roles for this team

        if (!saveChanges) return ServiceResult.Success();
        
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return ServiceResult.Success();
    }

    /// <summary>
    /// If a UserRole is created, updated or deleted then refresh all the roles for the user.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tableName"></param>
    /// <param name="idFieldName"></param>
    /// <returns></returns>
    private bool HandleEntity(BulkServiceContext context, string tableName, string idFieldName)
    {
        var saveChanges = false;
        var ids = context.AllServiceContexts.Where(x => x.TableName == tableName)
            .Select(x => x.Entity?.GetValue<Guid>(idFieldName) ?? throw new Exception("Unable to convert data operation"))
            .ToList();
        
        ids.AddRange(context.AllServiceContexts.Where(x => x.TableName == tableName)
            .Where(x => x.PreEntity != null)
            .Select(x => x.PreEntity?.GetValue<Guid>(idFieldName) ?? throw new Exception("Unable to convert data operation")));
        
        ids = ids.Distinct().ToList();
        
        foreach (var id in ids)
        {
            CreateSystemRecord(context, $"Update,{tableName},{id}");
            saveChanges = true;
        }
        
        return saveChanges;
    }

    /// <summary>
    /// Only create records for deletes
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tableName"></param>
    /// <param name="idFieldName"></param>
    /// <returns></returns>
    private bool HandleDelete(BulkServiceContext context, string tableName, string idFieldName)
    {
        var saveChanges = false;
        var deletes = context.AllServiceContexts.Where(x => x.TableName == tableName)
            .Where(x => x.DataOperation is Delete)
            .Select(x => x.Entity?.GetValue<Guid>(idFieldName) ?? throw new Exception("Unable to convert data operation"))
            .ToList();

        foreach (var id in deletes)
        {
            CreateSystemRecord(context, $"Delete,{tableName},{id}");
            saveChanges = true;
        }
        
        return saveChanges;
    }
    

    /// <summary>
    /// If a permission is deleted remove it from RolePermissions, if updated provide the old and new value
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private bool HandlePermission(BulkServiceContext context)
    {
        var saveChanges = false;
        var updates = context.AllServiceContexts.Where(x => x.TableName == "Permission")
            .Where(x => x.DataOperation is Update)
            .Select(x => new
            {
                x.DataOperation,
                EntityName = x.Entity?.GetValue<string>("Name"),
                PreEntityName = x.PreEntity?.GetValue<string>("Name"),
            });
        foreach (var permission in updates)
        {
            CreateSystemRecord(context, $"{permission.DataOperation},Permission,{permission.PreEntityName},{permission.EntityName}");
            saveChanges = true;
        }
        
        var deletes = context.AllServiceContexts.Where(x => x.TableName == "Permission")
            .Where(x => x.DataOperation is Delete)
            .Select(x => x.GetValue<string>("Name"));
        
        foreach (var permissionName in deletes)
        {
            CreateSystemRecord(context, $"Delete,{permissionName}");
            saveChanges = true;
        }
        return saveChanges;
    }

    private void CreateSystemRecord(BulkServiceContext context, string value)
    {
        var systemMetadata = Cache.Instance.GetTableMetadata("System");
        var db = context.GetDbContext<IXamsDbContext>();
        Dictionary<string, dynamic> systemRecord = new Dictionary<string, dynamic>();
        systemRecord["SystemId"] = Guid.NewGuid();
        systemRecord["Name"] = $"SECURITY_CACHE";
        systemRecord["Value"] = value;
        systemRecord["DateTime"] = DateTime.UtcNow;
        db.Add(EntityUtil.DictionaryToEntity(systemMetadata.Type, systemRecord));
    }
}