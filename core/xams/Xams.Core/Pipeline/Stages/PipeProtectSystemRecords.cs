using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeProtectSystemRecords : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = ProtectSystemRecords(context);
        if (!response.Succeeded)
        {
            return response;
        }
        
        return await base.Execute(context);
    }
    
    private Response<object?> ProtectSystemRecords(PipelineContext context)
    {
        if (context.TableName == "RolePermission")
        {
            if (context.DataOperation is DataOperation.Update or DataOperation.Delete)
            {
                var idProperty = context.Entity?.GetType().GetProperty("RoleId");
                Guid roleId = (Guid)(idProperty?.GetValue(context.Entity) ?? Guid.Empty);
                if (roleId == SystemRecords.SystemAdministratorRoleId)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot modify system role permissions."
                    };
                }
            }
            
        }
        if (context.TableName == "Permission")
        {
            if (context.DataOperation is DataOperation.Update or DataOperation.Delete)
            {
                var tagProperty = context.PreEntity.GetType().GetProperty("Tag");
                if (tagProperty != null)
                {
                    string? tag = tagProperty.GetValue(context.PreEntity) as string ?? "";
                    if (tag.ToLower() == "system")
                    {
                        return new Response<object?>()
                        {
                            Succeeded = false,
                            FriendlyMessage = $"Cannot {context.DataOperation.ToString().ToLower()} system record."
                        };
                    }
                }
            }
            
        }

        if (new[] { "User", "Role", "Team", "UserRole", "TeamRole", "TeamUser" }.Contains(context.TableName))
        {
            if (context.DataOperation is DataOperation.Update or DataOperation.Delete)
            {
                string primaryKey = Cache.Instance
                    .GetTableMetadata(context.TableName)!
                    .PrimaryKey;
                object id = null;
                if (context.DataOperation is DataOperation.Update)
                {
                    var property = context.PreEntity.GetType().GetProperty(primaryKey);
                    id = property?.GetValue(context.PreEntity);    
                } 
                else if (context.DataOperation is DataOperation.Delete)
                {
                    id = context.Entity!.GetType().GetProperty(primaryKey)!.GetValue(context.Entity)!;    
                }
                
                // Check if the ID is a Guid and matches any of the system record IDs
                if (id is Guid guidId && new[]
                    {
                        SystemRecords.SystemAdministratorRoleId,
                        SystemRecords.SystemUserRoleId,
                        SystemRecords.SystemAdministratorsTeamRoleId,
                        SystemRecords.SystemTeamUserId
                    }.Contains(guidId))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot {context.DataOperation.ToString().ToLower()} system record."
                    };
                }
            }

            if (context.DataOperation is DataOperation.Delete)
            {
                object idObj = context.Entity?.GetId() ??
                          throw new Exception($"Missing ID for {context.TableName}");
                
                // Check if the ID is a Guid and matches any of the system record IDs
                if (idObj is Guid id && new[]
                    {
                        SystemRecords.SystemUserId,
                        SystemRecords.SystemAdministratorTeamId,
                    }.Contains(id))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot {context.DataOperation.ToString().ToLower()} system record."
                    };
                }
            }
        }

        if (context.TableName == "Job")
        {
            if (context.DataOperation is DataOperation.Create or DataOperation.Delete)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Cannot {context.DataOperation.ToString().ToLower()} system record."
                };
            }
        }

        return new Response<object?>()
        {
            Succeeded = true
        };
    }
}
