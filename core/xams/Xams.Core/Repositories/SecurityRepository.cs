using System.Linq.Dynamic.Core;
using System.Text.Json;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Repositories;

public class SecurityRepository 
{
        
    public async Task<Response<object?>> Get(PermissionsInput permissionsInput, Guid userId)
    {
        try
        {
            if (permissionsInput.method == "has_permissions")
            {
                return await UserPermissions(permissionsInput, userId);
            }

            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "Invalid method"
            };
        }
        catch (Exception ex)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = ex.Message,
                LogMessage = $"{ex.InnerException?.Message ?? ex.Message}\n---StackTrace---\n{ex.StackTrace}\n---Inner StackTrace---\n{ex.InnerException?.StackTrace}",
            };
        }
    }
    public async Task<Response<object?>> UserPermissions(PermissionsInput permissionsInput, Guid userId)
    {
        if (permissionsInput.parameters == null)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "Missing parameters"
            };
        }
            
        if (!permissionsInput.parameters.TryGetValue("permissionNames", out var parameter))
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "Missing permissionNames parameter"
            };
        }
        string jsonString = parameter.ToString();
        string[]? permissionsNamesArray = JsonSerializer.Deserialize<string[]>(jsonString);
            
        var permissions = await PermissionCache.GetUserPermissions(userId, permissionsNamesArray);
            
        return new Response<object?>()
        {
            Succeeded = true,
            Data = permissions
        };
    }

    /// <summary>
    /// Returns true if 2 users are reachable through the same team.
    /// </summary>
    /// <param name="user1Id"></param>
    /// <param name="user2Id"></param>
    /// <returns></returns>
    public bool UsersExistInSameTeam(Guid user1Id, Guid user2Id)
    {
        var user1Teams = PermissionCache.UserTeams[user1Id];
        var user2Teams = PermissionCache.UserTeams[user2Id];

        foreach (var teamId in user1Teams)
        {
            if (user2Teams.Contains(teamId))
            {
                return true;
            }
        }

        return false;
    }
}