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
                Guid id = Guid.Empty;
                if (context.DataOperation is DataOperation.Update)
                {
                    var property = context.PreEntity.GetType().GetProperty(primaryKey);
                    id = (Guid)(property?.GetValue(context.PreEntity) ?? Guid.Empty);    
                } 
                else if (context.DataOperation is DataOperation.Delete)
                {
                    id = (Guid)context.Entity!.GetType().GetProperty(primaryKey)!.GetValue(context.Entity)!;    
                }
                
                if (new[]
                    {
                        SystemRecords.SystemAdministratorRoleId,
                        SystemRecords.SystemUserRoleId,
                        SystemRecords.SystemAdministratorsTeamRoleId,
                        SystemRecords.SystemTeamUserId
                    }.Contains(id))
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
                var metadata = Cache.Instance.GetTableMetadata(context.TableName);
                Guid id = context.Entity?.GetIdValue(metadata.Type) ??
                          throw new Exception($"Missing ID for {context.TableName}");
                if (new[]
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