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

        // Check custom owning user fields
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        foreach (var owningUserField in metadata.OwningUserFields)
        {
            var owningUserFieldProperty = context.PreEntity.GetType().GetProperty(owningUserField);
            Guid? owningUserFieldValue = owningUserFieldProperty?.GetValue(context.PreEntity) as Guid?;

            if (highestPermission is Permissions.PermissionLevel.User or Permissions.PermissionLevel.Team &&
                owningUserFieldValue == context.UserId)
            {
                return new Response<T>()
                {
                    Succeeded = true
                };
            }
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

        // Get custom owning user fields
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var customOwningUserFields = new Dictionary<string, Guid?>();
        foreach (var owningUserField in metadata.OwningUserFields)
        {
            var property = context.Entity.GetType().GetProperty(owningUserField);
            customOwningUserFields[owningUserField] = property?.GetValue(context.Entity) as Guid?;
        }

        // No assignment fields, skip
        if (owningTeamProperty == null && owningUserProperty == null && customOwningUserFields.Count == 0)
        {
            return new Response<T>()
            {
                Succeeded = true,
            };
        }

        // If this is an update and the user hasn't changed the owning team\user or custom owning user fields, allow the update
        if (context.PreEntity != null)
        {
            var existingOwningTeamProperty = context.PreEntity.GetType().GetProperty("OwningTeamId");
            var existingOwningUserProperty = context.PreEntity.GetType().GetProperty("OwningUserId");
            Guid? existingOwningTeamId = existingOwningTeamProperty?.GetValue(context.PreEntity) as Guid?;
            Guid? existingOwningUserId = existingOwningUserProperty?.GetValue(context.PreEntity) as Guid?;

            bool owningFieldsChanged = owningTeamId != existingOwningTeamId || owningUserId != existingOwningUserId;

            // Check if any custom owning user fields changed
            foreach (var kvp in customOwningUserFields)
            {
                var existingProperty = context.PreEntity.GetType().GetProperty(kvp.Key);
                Guid? existingValue = existingProperty?.GetValue(context.PreEntity) as Guid?;
                if (kvp.Value != existingValue)
                {
                    owningFieldsChanged = true;
                    break;
                }
            }

            if (!owningFieldsChanged)
            {
                return new Response<T>()
                {
                    Succeeded = true
                };
            }
        }

        // Check if at least one owning field is set
        bool hasOwningField = owningTeamId != null || owningUserId != null;

        if ((owningTeamProperty != null || owningUserProperty != null) && !hasOwningField)
        {
            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage = $"Record must be assigned to a team or user."
            };
        }

        // Get highest permission level
        Permissions.PermissionLevel? highestPermission = Permissions.GetHighestPermission(permissions);

        // System permission: Allow assignment to any team/user
        if (highestPermission is Permissions.PermissionLevel.System)
        {
            return new Response<T>()
            {
                Succeeded = true
            };
        }

        // Team permission: Allow assignment to any user + teams user belongs to
        if (highestPermission is Permissions.PermissionLevel.Team)
        {
            // Validate team assignment
            if (owningTeamId != null)
            {
                List<Guid> userTeams = PermissionCache.UserTeams[context.UserId];
                if (!userTeams.Any(x => x == owningTeamId))
                {
                    return new Response<T>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Cannot assign to team. You are not a member of this team."
                    };
                }
            }

            // Validate user assignments (just check user exists)
            if (owningUserId != null)
            {
                Response<object?> userResponse = await context.DataRepository.Find("User", owningUserId.Value, false);
                if (userResponse.Data == null)
                {
                    return new Response<T>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Could not find user with ID {owningUserId}."
                    };
                }
            }

            // Validate custom owning user fields (just check users exist)
            foreach (var kvp in customOwningUserFields)
            {
                if (kvp.Value != null)
                {
                    Response<object?> userResponse = await context.DataRepository.Find("User", kvp.Value.Value, false);
                    if (userResponse.Data == null)
                    {
                        return new Response<T>()
                        {
                            Succeeded = false,
                            FriendlyMessage = $"Could not find user with ID {kvp.Value}."
                        };
                    }
                }
            }

            return new Response<T>()
            {
                Succeeded = true
            };
        }

        // User permission: Allow assignment to any user (cannot assign teams)
        if (highestPermission is Permissions.PermissionLevel.User)
        {
            // Reject team assignment
            if (owningTeamId != null)
            {
                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Cannot assign to team. You need Team-level assign permissions."
                };
            }

            // Validate user assignments (just check user exists)
            if (owningUserId != null)
            {
                Response<object?> userResponse = await context.DataRepository.Find("User", owningUserId.Value, false);
                if (userResponse.Data == null)
                {
                    return new Response<T>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"Could not find user with ID {owningUserId}."
                    };
                }
            }

            // Validate custom owning user fields (just check users exist)
            foreach (var kvp in customOwningUserFields)
            {
                if (kvp.Value != null)
                {
                    Response<object?> userResponse = await context.DataRepository.Find("User", kvp.Value.Value, false);
                    if (userResponse.Data == null)
                    {
                        return new Response<T>()
                        {
                            Succeeded = false,
                            FriendlyMessage = $"Could not find user with ID {kvp.Value}."
                        };
                    }
                }
            }

            return new Response<T>()
            {
                Succeeded = true
            };
        }

        // No assign permissions: Can only assign to self
        if (owningTeamId != null)
        {
            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage = $"Cannot assign to team. You need assign permissions."
            };
        }

        if (owningUserId != null && owningUserId != context.UserId)
        {
            return new Response<T>()
            {
                Succeeded = false,
                FriendlyMessage = $"You can only assign records to yourself."
            };
        }

        foreach (var kvp in customOwningUserFields)
        {
            if (kvp.Value != null && kvp.Value != context.UserId)
            {
                return new Response<T>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"You can only assign records to yourself."
                };
            }
        }

        return new Response<T>()
        {
            Succeeded = true
        };
    }
    
    private async Task<Response<object?>> CheckSecurity(PipelineContext context)
    {
        // Does the user have permission to perform the operation on the table?
        context.Permissions = await GetTablePermissions(context);
        var tablePermissionCheck = await TablePermissionCheck(context);
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

    private async Task<Response<object?>> TablePermissionCheck(PipelineContext context)
    {
        if (context.DataOperation is DataOperation.Read)
        {
            string[] tables = QueryUtil.GetTables(context.ReadInput);
            var tablePermissions = context.Permissions.ToTablePermissions(); 
            
            // If the teamsView parameter is set to true then only return records
            // the user can view due to their team ownership
            if (context.InputParameters.ContainsKey("teamsView") &&
                context.InputParameters["teamsView"].GetBoolean())
            {
                for (int i = 0; i < context.Permissions.Length; i++)
                {
                    if (context.Permissions[i] == $"TABLE_{context.ReadInput?.tableName}_READ_SYSTEM")
                    {
                        context.Permissions[i] = $"TABLE_{context.ReadInput?.tableName}_READ_TEAM";
                    }
                }
            }
            
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

        if (context.Permissions.Length == 0)
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