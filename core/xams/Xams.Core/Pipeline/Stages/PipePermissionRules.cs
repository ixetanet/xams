using System.Linq.Dynamic.Core;
using System.Reflection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipePermissionRules : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        if (context.TableName == "Permission")
        {
            var response = await PermissionRules(context);
            if (!response.Succeeded)
            {
                return response;
            }
        }
        
        if (context.TableName == "RolePermission")
        {
            var response = await PermissionRoleRules(context);
            if (!response.Succeeded)
            {
                return response;
            }
        }

        var pipelineResponse = await base.Execute(context);

        // Create RolePermission for System Admin
        if (context.TableName == "Permission")
        {
            var response = await CreateSystemAdminRolePermission(context);
            if (!response.Succeeded)
            {
                return response;
            }
        }

        return pipelineResponse;
    }

    private async Task<Response<object?>> PermissionRules(PipelineContext context)
    {
        if (context.DataOperation is DataOperation.Create or DataOperation.Update)
        {
            var nameProperty = context.Entity?.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                string? name = nameProperty.GetValue(context.Entity) as string ?? "";
                if (name.StartsWith("TABLE"))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot create permissions that start with TABLE."
                    };
                }

                if (name.StartsWith("ACTION"))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot create permissions that start with ACTION."
                    };
                }

                if (name.StartsWith("JOB"))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot create permissions that start with JOB."
                    };
                }

                if (name.Contains(" "))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Permission name cannot contain spaces."
                    };
                }

                // Regex to allow only alphanumeric characters and underscores
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-zA-Z0-9_]*$"))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Permission name can only contain alphanumeric characters and underscores."
                    };
                }

                if (!string.IsNullOrEmpty(name))
                {
                    IXamsDbContext dbContext = context.DataRepository.CreateNewDbContext();
                    var metaData = Cache.Instance.GetTableMetadata(context.TableName);
                    DynamicLinq dynamicLinq =
                        new DynamicLinq(dbContext, metaData.Type);
                    IQueryable query = dynamicLinq.Query;
                    var existingPermission = await query.Where("Name == @0", name).ToDynamicListAsync();
                    if (existingPermission.Count > 0)
                    {
                        return new Response<object?>()
                        {
                            Succeeded = false,
                            FriendlyMessage = $"Permission with name {name} already exists."
                        };
                    }
                }
            }
        }


        return new Response<object?>()
        {
            Succeeded = true
        };
    }

    private async Task<Response<object?>> PermissionRoleRules(PipelineContext context)
    {
        // Only allow a single table permission for a role, and don't allow duplicate permissions
        if (context.DataOperation is DataOperation.Create)
        {
            var roleIdProperty = context.Entity?.GetType().GetProperty("RoleId");
            var permissionIdProperty = context.Entity?.GetType().GetProperty("PermissionId");
            if (roleIdProperty != null && permissionIdProperty != null)
            {
                Guid roleId = (Guid)roleIdProperty.GetValue(context.Entity)!;
                Guid permissionId = (Guid)permissionIdProperty.GetValue(context.Entity)!;
                
                IXamsDbContext dbContext = context.DataRepository.CreateNewDbContext();
                
                var permissionMetadata = Cache.Instance.GetTableMetadata("Permission");
                var roleMatadata = Cache.Instance.GetTableMetadata("Role");
                var rolePermissionMetadata = Cache.Instance.GetTableMetadata("RolePermission");
                
                // First get the permission we're trying to create
                DynamicLinq linqPermission = new DynamicLinq(dbContext, permissionMetadata.Type);
                IQueryable permissionQuery = linqPermission.Query;
                permissionQuery = permissionQuery.Where("PermissionId == @0", permissionId);
                var permission = (await permissionQuery.ToDynamicListAsync()).FirstOrDefault();
                
                if (permission == null)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage =  $"Permission with id {permissionId} does not exist."
                    };
                }
                
                string permissionName = (string)permission.GetType().GetProperty("Name")!.GetValue(permission)!;
                bool isTable = permissionName.StartsWith("TABLE_");
                string permissionNameWithoutLevel = permissionName;
                if (permissionName.EndsWith("_USER"))
                {
                    permissionNameWithoutLevel = permissionName.Substring(0, permissionName.Length - 5);
                } 
                else if (permissionName.EndsWith("_TEAM"))
                {
                    permissionNameWithoutLevel = permissionName.Substring(0, permissionName.Length - 5);
                }
                else if (permissionName.EndsWith("_SYSTEM"))
                {
                    permissionNameWithoutLevel = permissionName.Substring(0, permissionName.Length - 7);
                }
                
                // Check to see if a table permission already exists 
                DynamicLinq linqRole = new DynamicLinq(dbContext, roleMatadata.Type);
                DynamicLinq linqRolePermission =
                    new DynamicLinq(dbContext, rolePermissionMetadata.Type);
                IQueryable query = linqRole.Query;
                query = query.Join(linqRolePermission.Query, "RoleId", "RoleId", "inner");
                query = query.Join(linqPermission.Query, "PermissionId", "PermissionId", "new (outer as outer, inner as inner)");
                query = query.Where("outer.RoleId == @0", roleId);
                if (isTable)
                {
                    query = query.Where("inner.Name.StartsWith(@0)", permissionNameWithoutLevel);
                }
                else
                {
                    query = query.Where("inner.Name == @0", permissionName);    
                }
                
                var existingPermission = await query.ToDynamicListAsync();
                if (existingPermission.Count > 0)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = isTable ? $"Permission for {permissionNameWithoutLevel} already exists." 
                            : $"Permission with name {permissionName} already exists."
                    };
                }
            }
        }
        return ServiceResult.Success();
    }

    private async Task<Response<object?>> CreateSystemAdminRolePermission(PipelineContext context)
    {
        if (context.DataOperation is DataOperation.Create)
        {
            IXamsDbContext db = context.DataRepository.GetDbContext<IXamsDbContext>();
            var metaData = Cache.Instance.GetTableMetadata("RolePermission").Type;
            var rolePermission = Activator.CreateInstance(metaData);
            metaData.GetProperty("RolePermissionId")?.SetValue(rolePermission, Guid.NewGuid());
            metaData.GetProperty("RoleId")?.SetValue(rolePermission, SystemRecords.SystemAdministratorRoleId);
            var permissionId = context.Entity?.GetValue<Guid>("PermissionId"); 
            metaData.GetProperty("PermissionId")?.SetValue(rolePermission, permissionId);
            db.Add(rolePermission!);
            await db.SaveChangesAsync();
        }

        return ServiceResult.Success();
    }
}