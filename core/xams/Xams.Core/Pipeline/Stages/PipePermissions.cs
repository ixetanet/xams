using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

/// <summary>
/// Determines if the user can perform the Create\Read\Update\Delete operation
/// </summary>
public class PipePermissions : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await CheckSecurity(context);
        if (!response.Succeeded)
        {
            return response;
        }
        
        return await base.Execute(context);
    }
    
    
    private async Task<Response<T>> AccessPermissionCheck<T>(PipelineContext context)
    {
        string[] permissions = await Permissions.GetUserTablePermissions(context.UserId, context.TableName,
            [context.DataOperation.ToString().ToUpper()]); 
        
        Permissions.PermissionLevel? highestPermission = Permissions.GetHighestPermission(permissions);
        if (highestPermission is Permissions.PermissionLevel.System)
        {
            return new Response<T>()
            {
                Succeeded = true
            };
        }

        var owningTeamProperty = context.PreEntity.GetType().GetProperty("OwningTeamId");
        Guid? owningTeamId = owningTeamProperty?.GetValue(context.PreEntity) as Guid?;

        if (highestPermission is Permissions.PermissionLevel.Team && owningTeamId != null)
        {
            // Get the users teams, then check if the owning team is one of the users teams
            List<Guid> userTeams = PermissionCache.UserTeams[context.UserId];
            //(await context.SecurityRepository.UserTeams(context.UserId)).Data;
            if (userTeams.Any(x => x == owningTeamId))
            {
                return new Response<T>()
                {
                    Succeeded = true
                };
            }
        }

        var owningUserProperty = context.PreEntity.GetType().GetProperty("OwningUserId");
        Guid? owningUserId = owningUserProperty?.GetValue(context.PreEntity) as Guid?;

        if (highestPermission is Permissions.PermissionLevel.User or Permissions.PermissionLevel.Team &&
            owningUserId == context.UserId)
        {
            return new Response<T>()
            {
                Succeeded = true
            };
        }

        return new Response<T>()
        {
            Succeeded = false,
            FriendlyMessage =
                $"Insufficient permissions to {context.DataOperation.ToString().ToLower()} {context.TableName}."
        };
    }

    private async Task<Response<T>> AssignPermissionCheck<T>(PipelineContext context)
    {
        var permissions = await Permissions.GetUserTablePermissions(context.UserId, context.TableName, ["ASSIGN"]);

        var owningTeamProperty = context.Entity.GetType().GetProperty("OwningTeamId");
        var owningUserProperty = context.Entity.GetType().GetProperty("OwningUserId");
        Guid? owningTeamId = owningTeamProperty?.GetValue(context.Entity) as Guid?;
        Guid? owningUserId = owningUserProperty?.GetValue(context.Entity) as Guid?;
        
        // No assignment fields, skip
        if (owningTeamProperty == null && owningUserProperty == null)
        {
            return new Response<T>()
            {
                Succeeded = true,
            };
        }

        // If this is an update and the user hasn't changed the owning team\user, allow the update
        if (context.PreEntity != null)
        {
            var existingOwningTeamProperty = context.PreEntity.GetType().GetProperty("OwningTeamId");
            var existingOwningUserProperty = context.PreEntity.GetType().GetProperty("OwningUserId");
            Guid? existingOwningTeamId = existingOwningTeamProperty?.GetValue(context.PreEntity) as Guid?;
            Guid? existingOwningUserId = existingOwningUserProperty?.GetValue(context.PreEntity) as Guid?;

            if (owningTeamId == existingOwningTeamId && owningUserId == existingOwningUserId)
            {
                return new Response<T>()
                {
                    Succeeded = true
                };
            }
        }

        if ((owningTeamProperty != null || owningUserProperty != null) && owningTeamId == null &&
            owningUserId == null)
        {
            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage = $"Record must be assigned to a team or user."
            };
        }

        // If system permission, allow assigment to any team\user
        Permissions.PermissionLevel? highestPermission = Permissions.GetHighestPermission(permissions);
        if (highestPermission is Permissions.PermissionLevel.System)
        {
            return new Response<T>()
            {
                Succeeded = true
            };
        }

        // If team permission, allow assignment to all teams the user is a member of
        if (owningTeamId != null)
        {
            if (highestPermission is Permissions.PermissionLevel.Team)
            {
                // Get the users teams, then check if the owning team is one of the users teams
                List<Guid> userTeams = PermissionCache.UserTeams[context.UserId];
                // make the below check more readable
                bool isTeamAccessible = userTeams.Any(x => x == owningTeamId);
                if (isTeamAccessible)
                {
                    return new Response<T>()
                    {
                        Succeeded = true
                    };
                }

                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Cannot assign to team, No membership."
                };
            }

            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage =
                    $"Cannot assign to team, can only assign to self."
            };
        }

        if (owningUserId != null)
        {
            Response<ReadOutput> owningUserResponse =
                await context.DataRepository.Find("User", owningUserId.Value, false);

            var userRecord = owningUserResponse.Data?.results[0];
            if (userRecord == null)
            {
                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Could not find user with ID {owningUserId}."
                };
            }

            string userName = userRecord.GetType().GetProperty("Name")?.GetValue(userRecord) as string ?? "";

            if (context.UserId == owningUserId)
            {
                return new Response<T>()
                {
                    Succeeded = true
                };
            }

            if (highestPermission is Permissions.PermissionLevel.Team)
            {
                if (context.SecurityRepository.UsersExistInSameTeam(context.UserId, owningUserId.Value))
                {
                    return new Response<T>()
                    {
                        Succeeded = true
                    };
                }

                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"User {userName} is not in one of your teams."
                };
            }

            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage = $"Cannot assign {context.TableName} to user {userName}. Can only assign to self."
            };
        }

        return new Response<T>()
        {
            Succeeded = false,
            FriendlyMessage = $"Record must be assigned to a user.",
            Data = default
        };
    }
    
    private async Task<Response<object?>> CheckSecurity(PipelineContext context)
    {
        // Does the user have permission to perform the operation on the table?
        var tablePermissions = await GetTablePermissions(context);
        var tablePermissionCheck = await TablePermissionCheck(context, tablePermissions);
        if (!tablePermissionCheck.Succeeded)
        {
            return tablePermissionCheck;
        }

        // Does the user have permission to update or delete the specific record?
        if (context.DataOperation is DataOperation.Update or DataOperation.Delete)
        {
            Response<object?> accessPermissionCheck =
                await AccessPermissionCheck<object?>(context);
            if (!accessPermissionCheck.Succeeded)
            {
                return accessPermissionCheck;
            }
        }

        // Does the user have permission to assign the record to the specified team or user?
        if (context.DataOperation is DataOperation.Create or DataOperation.Update)
        {
            Response<object?> assignPermissionCheck = await AssignPermissionCheck<object?>(context);
            if (!assignPermissionCheck.Succeeded)
            {
                return assignPermissionCheck;
            }
        }

        return new Response<object?>()
        {
            Succeeded = true
        };
    }

    private async Task<Response<object?>> TablePermissionCheck(PipelineContext context, string[] permissions)
    {
        if (context.DataOperation is DataOperation.Read)
        {
            string[] tables = QueryUtil.GetTables(context.ReadInput);
            var tablePermissions = permissions.ToTablePermissions(); 
            foreach (var table in tables)
            {
                if (tablePermissions.FirstOrDefault(x => x.Table == table) == null)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Missing {context.DataOperation.ToString().ToLower()} permissions for {table}."
                    };
                }
            }
            
            return new Response<object?>()
            {
                Succeeded = true
            };
            
        }

        if (permissions.Length == 0)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Missing {context.DataOperation.ToString().ToLower()} permissions for {context.TableName}."
            };
        }
        
        return new Response<object?>()
        {
            Succeeded = true
        };
    }

    /// <summary>
    /// Retrieve and or provide additional security privileges depending on the type of call
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task<string[]> GetTablePermissions(PipelineContext context)
    {
        string[] permissions;
        if (context.DataOperation is DataOperation.Read)
        {
            string[] tables = QueryUtil.GetTables(context.ReadInput);
            // If this is a read on OwningUserId or OwningTeamId then we need to check the "assign" permissions
            if (new[] { "User", "Team" }.Contains(context.ReadInput?.tableName) &&
                context.InputParameters.ContainsKey("isOwnerField") && context.InputParameters["isOwnerField"].GetBoolean() &&
                context.InputParameters.ContainsKey("tableName") &&
                context.InputParameters["tableName"].GetString() != null &&
                (context.ReadInput!.joins == null || context.ReadInput.joins.Length == 0))
            {
                string owningTableName = context.InputParameters["tableName"].GetString()!;
                permissions = await PermissionCache.GetUserPermissions(
                    context.UserId,
                    [
                        $"TABLE_{owningTableName}_ASSIGN_USER",
                        $"TABLE_{owningTableName}_ASSIGN_TEAM",
                        $"TABLE_{owningTableName}_ASSIGN_SYSTEM"
                    ]);
    
                var highestPermission = Permissions.GetHighestPermission(permissions);
                if (highestPermission != null)
                {
                    permissions = [$"TABLE_{context.ReadInput.tableName}_READ_{highestPermission.ToString()?.ToUpper()}"];
                }
                else
                {
                    // If no assign permissions, fall back to user read permissions and select none
                    permissions = [$"TABLE_{context.ReadInput.tableName}_READ_USER"];
                    // Manipulate the query so that it returns no results
                    context.ReadInput.filters =
                    [
                        new()
                        {
                            field = $"{context.ReadInput.tableName}Id",
                            @operator = "==",
                            value = Guid.Empty.ToString()
                        }
                    ];
                }
                
            }
            else
            {
                permissions = await Permissions.GetUserTablePermissions(
                    context.UserId,
                    tables, context.DataOperation.ToString().ToUpper());
            }
            
            
        }
        else
        {
            permissions = await Permissions.GetUserTablePermissions(context.UserId, context.TableName, 
                [context.DataOperation.ToString().ToUpper()]);
        }

        return permissions;
    }

    
}