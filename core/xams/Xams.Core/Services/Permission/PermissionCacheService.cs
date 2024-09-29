using System.Globalization;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Startup;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;

namespace Xams.Core.Services.Permission;

/// <summary>
/// Whenever the permissions are updated, this service will invalidate the cache so all running instances will refresh their cache.
/// </summary>
[BulkService(BulkStage.Post)]
public class PermissionCacheService : IBulkService
{
    private static readonly string[] SecurityEntities = ["Permission", "Role", "RolePermission", "UserRole", "TeamUser", "TeamRole"];
    private static readonly string[] AllSecurityEntities = SecurityEntities.Concat(new[] { "User", "Team" }).ToArray();
    
    public async Task<Response<object?>> Execute(BulkServiceContext context)
    {
        if (context.AllServiceContexts.All(x => x.DataOperation is Read))
        {
            return ServiceResult.Success();
        }
        
        if (!context.AllServiceContexts.Any(x => AllSecurityEntities.Contains(x.TableName)))
        {
            return ServiceResult.Success();
        }

        var invalidateCache = context.AllServiceContexts
            .Any(x => x.DataOperation is Delete && x.TableName is "User" or "Team");

        if (!invalidateCache)
        {
            invalidateCache = context.AllServiceContexts
                .Any(x => x.DataOperation is Create or Update or Delete && SecurityEntities.Contains(x.TableName));    
        }
        
        if (!invalidateCache)
        {
            return ServiceResult.Success();
        }

        var systemMetadata = Cache.Instance.GetTableMetadata("System");
        var db = context.GetDbContext<BaseDbContext>();

        Dictionary<string, dynamic> systemRecord = new();
        systemRecord["SystemId"] = GuidUtil.FromString(SystemRecords.CachePermissionsLastUpdateSystem);
        systemRecord["Name"] = SystemRecords.CachePermissionsLastUpdateSystem;
        systemRecord["Value"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        var entity = EntityUtil.DictionaryToEntity(systemMetadata.Type, systemRecord);
        db.Update(entity);
        await db.SaveChangesAsync();
        
        return ServiceResult.Success();
    }
}