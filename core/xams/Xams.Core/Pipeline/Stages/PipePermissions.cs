using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipePermissions : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await CheckSecurity(context);
        if (!response.Succeeded)
        {
            return response;
        }

        context.TablePermissions = (List<TablePermission>)response.Data!;
        return await base.Execute(context);
    }

    private async Task<Response<T>> AccessPermissionCheck<T>(PipelineContext context)
    {
        string[] permissions = context.TablePermissions
            .Where(x => x.Table == context.TableName)
            .SelectMany(x => x.Permissions)
            .Where(x => x.EndsWith($"{context.DataOperation.ToString().ToUpper()}_USER") || 
                        x.EndsWith($"{context.DataOperation.ToString().ToUpper()}_TEAM") || 
                        x.EndsWith($"{context.DataOperation.ToString().ToUpper()}_SYSTEM"))
            .ToArray();
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
            List<Guid>? userTeams = (await context.SecurityRepository.UserTeams(context.UserId)).Data;
            if (userTeams != null && userTeams.Any(x => x == owningTeamId))
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
        string[] permissions = [];
        if (context.TablePermissions == null)
        {
            throw new Exception($"Table permissions not set in {nameof(PipePermissions)}. Cannot check assign permissions.");
        }

        permissions = context.TablePermissions.SelectMany(x => x.Permissions)
            .Where(x => x.StartsWith($"TABLE_{context.TableName}_ASSIGN"))
            .Select(x => x).ToArray();

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
            object? team = (await context.SecurityRepository.Team(owningTeamId.Value)).Data;

            if (team == null)
            {
                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Could not find team with ID {owningTeamId}."
                };
            }

            if (highestPermission is Permissions.PermissionLevel.Team)
            {
                // Get the users teams, then check if the owning team is one of the users teams
                List<Guid>? userTeams = (await context.SecurityRepository.UserTeams(context.UserId)).Data;
                // make the below check more readable
                bool isTeamAccessible = userTeams != null && userTeams.Any(x => x == owningTeamId);
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
                    FriendlyMessage = $"No membership to team {team.GetType().GetProperty("Name").GetValue(team)}."
                };
            }

            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage =
                    $"Cannot assign {context.TableName} to team {team.GetType().GetProperty("Name").GetValue(team)}. Can only assign to self."
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
                if ((await context.SecurityRepository.UsersExistInSameTeam(context.UserId, (Guid)owningUserId)).Data)
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
        context.TablePermissions = await GetTablePermissions(context);
        if (!TablePermissionCheck(context, out Response<object?> repositoryResponse))
        {
            return repositoryResponse;
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
            Succeeded = true,
            Data = context.TablePermissions
        };
    }

    private bool TablePermissionCheck<T>(PipelineContext context, out Response<T?> response)
    {
        if (context.DataOperation is DataOperation.Read)
        {
            string[] tables = QueryUtil.GetTables(context.ReadInput);
            foreach (var table in tables)
            {
                if (context.TablePermissions.FirstOrDefault(x => x.Table == table) == null)
                {
                    response = new Response<T?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Missing {context.DataOperation.ToString().ToLower()} permissions for {table}."
                    };
                    return false;
                }
            }
            
            response = new Response<T?>()
            {
                Succeeded = true
            };

            return true;
        }

        if (context.TablePermissions.FirstOrDefault(x => x.Table == context.TableName) == null)
        {
            response = new Response<T?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Missing {context.DataOperation.ToString().ToLower()} permissions for {context.TableName}."
            };
            return false;
        }
        
        response = new Response<T?>()
        {
            Succeeded = true
        };
        
        return true;
    }

    private async Task<List<TablePermission>> GetTablePermissions(PipelineContext context)
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
                permissions = await Permissions.GetUserPermissions(context.DataRepository.CreateNewDbContext(),
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
                permissions = await Permissions.GetUserTablePermissions(context.DataRepository.CreateNewDbContext(),
                    context.UserId,
                    tables, context.DataOperation.ToString().ToUpper());
            }
        }
        else
        {
            if (context.TablePermissions == null)
            {
                throw new Exception($"Null table permissions in {nameof(PipePermissions)}.");
            }
            return context.TablePermissions;
        }

        return permissions.ToTablePermissions();
    }

    
}