using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Dynamic.Core;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
// ReSharper disable InconsistentNaming

namespace Xams.Core;

public static class PermissionCache
{
    public static DateTime LastUpdateTime = DateTime.UtcNow;
    public static IServiceProvider ServiceProvider { get; set; } = null!;
    
    private static ConcurrentDictionary<Guid, User> _users { get; set; } = new();
    /// <summary>
    /// Hashset of all the known users in the system. 
    /// </summary>
    public static ReadOnlyDictionary<Guid, User> Users => _users.AsReadOnly();
    private static ConcurrentDictionary<Guid, HashSet<string>> _rolePermissions { get; set; } = new();
    /// <summary>
    /// RoleId, Permission Names
    /// </summary>
    public static ReadOnlyDictionary<Guid, HashSet<string>> RolePermissions => _rolePermissions.AsReadOnly();
    
    private static ConcurrentDictionary<Guid, List<Guid>> _userRoles { get; set; } = new();
    /// <summary>
    /// UserId, RoleIds
    /// </summary>
    public static ReadOnlyDictionary<Guid, List<Guid>> UserRoles => _userRoles.AsReadOnly();
    
    private static ConcurrentDictionary<Guid, List<Guid>> _teamRoles { get; set; } = new();
    /// <summary>
    /// TeamId, RoleIds
    /// </summary>
    public static ReadOnlyDictionary<Guid, List<Guid>> TeamRoles => _teamRoles.AsReadOnly();
    
    private static ConcurrentDictionary<Guid, List<Guid>> _userTeams { get; set; } = new();
    /// <summary>
    /// UserId, TeamIds
    /// </summary>
    public static ReadOnlyDictionary<Guid, List<Guid>> UserTeams => _userTeams.AsReadOnly();
    
    // public static ReadOnlyDictionary<Guid, List<Guid>> ReadOnly => UserTeams.AsReadOnly();

    /// <summary>
    /// Cache known user ids. When a user is first created\given permissions in a multiserver environment, that user's
    /// permissions will not be immediately available on all servers. That's problematic while using a load balancer
    /// without sticky sessions enabled. If the user doesn't exist in the User cache then make a query to the database
    /// to retrieve permissions.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="userId"></param>
    public static async Task CacheUsers(IXamsDbContext db, Guid? userId = null)
    {
        var user = Cache.Instance.GetTableMetadata("User");
        var userQuery = new DynamicLinq(db, user.Type).Query;
        IQueryable query = userQuery;
        
        ConcurrentDictionary<Guid, User> users = new();
        if (userId != null)
        {
            query = query.Where("UserId == @0", userId.Value);
        }
        query = query.Select("new (UserId, CreatedDate)");
        
        var results = await query.ToDynamicListAsync();

        // The user doesn't exist
        if (results.Count == 0)
        {
            return;
        }
        
        foreach (var result in results)
        {
            users[result.UserId] = new User { CreatedDate = result.CreatedDate };
            if (!_userRoles.ContainsKey(result.UserId))
            {
                _userRoles[result.UserId] = new List<Guid>();
            }
            if (!_userTeams.ContainsKey(result.UserId))
            {
                _userTeams[result.UserId] = new List<Guid>();
            }
        }

        if (userId != null)
        {
            _users[userId.Value] = users[userId.Value];
        }
        else
        {
            _users = users;
        }
    }
    
    /// <summary>
    /// Call on start with no RoleId, call with RoleId when any RolePermission(s) have changed
    /// </summary>
    /// <param name="db"></param>
    /// <param name="roleId"></param>
    public static async Task CacheRolePermissions(IXamsDbContext db, Guid? roleId = null)
    {
        var permission = Cache.Instance.GetTableMetadata("Permission");
        var rolePermission = Cache.Instance.GetTableMetadata("RolePermission");
        
        var permissionQuery = new DynamicLinq(db, permission.Type).Query;
        var rolePermissionQuery = new DynamicLinq(db,rolePermission.Type).Query;

        IQueryable query = rolePermissionQuery
            .Join(permissionQuery, "PermissionId", "PermissionId",
                "new(outer as RolePermission, inner as Permission)");
        
        ConcurrentDictionary<Guid, HashSet<string>> rolePermissions = new();
        if (roleId != null)
        {
            rolePermissions[roleId.Value] = new HashSet<string>();
            query = query.Where("RolePermission.RoleId == @0", roleId.Value);
        }
        var results = await query.ToDynamicListAsync();
        
        foreach (var result in results)
        {
            Guid rid = result.RolePermission.RoleId;
            if (!rolePermissions.ContainsKey(rid))
            {
                rolePermissions[rid] = new HashSet<string>();    
            }
            
            rolePermissions[rid].Add(result.Permission.Name);
        }

        if (roleId != null)
        {
            _rolePermissions[roleId.Value] = rolePermissions[roleId.Value];
        }
        else
        {
            _rolePermissions = rolePermissions;
        }
    }

    /// <summary>
    /// Call on start with no UserId, call with UserId when role has been directly assigned\removed from a user
    /// </summary>
    /// <param name="db"></param>
    /// <param name="userId"></param>
    public static async Task CacheUserRoles(IXamsDbContext db, Guid? userId = null)
    {
        var userRole = Cache.Instance.GetTableMetadata("UserRole");
        var userRoleQuery = new DynamicLinq(db, userRole.Type).Query;

        ConcurrentDictionary<Guid, List<Guid>> userRoles = new();
        if (userId != null)
        {
            userRoles[userId.Value] = new List<Guid>();
            userRoleQuery = userRoleQuery.Where("UserId == @0", userId.Value);
        }
        
        var results = await userRoleQuery.ToDynamicListAsync();
        
        foreach (var result in results)
        {
            Guid uid = result.UserId;
            if (!userRoles.ContainsKey(uid))
            {
                userRoles[uid] = new List<Guid>();
            }
            userRoles[uid].Add(result.RoleId);
        }

        if (userId != null)
        {
            _userRoles[userId.Value] = userRoles[userId.Value];
        }
        else
        {
            _userRoles = userRoles;
        }
    }

    /// <summary>
    /// Call on start with no TeamId, call with TeamId when a role has been assigned\removed from a team
    /// </summary>
    /// <param name="db"></param>
    /// <param name="teamId"></param>
    public static async Task CacheTeamRoles(IXamsDbContext db, Guid? teamId = null)
    {
        var teamRole = Cache.Instance.GetTableMetadata("TeamRole");
        var teamRoleQuery = new DynamicLinq(db, teamRole.Type).Query;
        ConcurrentDictionary<Guid, List<Guid>> teamRoles = new();
        if (teamId != null)
        {
            teamRoles[teamId.Value] = new List<Guid>();
            teamRoleQuery = teamRoleQuery.Where("TeamId == @0", teamId.Value);
        }
        var results = await teamRoleQuery.ToDynamicListAsync();
        foreach (var result in results)
        {
            Guid tid = result.TeamId;
            if (!teamRoles.ContainsKey(tid))
            {
                teamRoles[tid] = new List<Guid>();
            }
            teamRoles[tid].Add(result.RoleId);
        }

        if (teamId != null)
        {
            _teamRoles[teamId.Value] = teamRoles[teamId.Value];
        }
        else
        {
            _teamRoles = teamRoles;
        }
    }

    /// <summary>
    /// Call on start with no UserId, call with UserId when a user has been added\removed from a team
    /// </summary>
    /// <param name="db"></param>
    /// <param name="userId"></param>
    public static async Task CacheUserTeams(IXamsDbContext db, Guid? userId = null)
    {
        var teamUser = Cache.Instance.GetTableMetadata("TeamUser");
        var teamUserQuery = new DynamicLinq(db, teamUser.Type).Query;
        ConcurrentDictionary<Guid, List<Guid>> teamUsers = new();
        if (userId != null)
        {
            teamUsers[userId.Value] = new List<Guid>();
            teamUserQuery = teamUserQuery.Where("UserId == @0", userId.Value);
        }
        var results = await teamUserQuery.ToDynamicListAsync();
        foreach (var result in results)
        {
            Guid uid = result.UserId;
            if (!teamUsers.ContainsKey(uid))
            {
                teamUsers[uid] = new List<Guid>();
            }
            teamUsers[uid].Add(result.TeamId);
        }

        if (userId != null)
        {
            _userTeams[userId.Value] = teamUsers[userId.Value];
        }
        else
        {
            _userTeams = teamUsers;
        }
    }
    
    public static async Task<string[]> GetUserPermissions(Guid userId, string[]? permissionNames = null)
    {
        HashSet<string> permissions = new();
        
        // If this user is not known by this server yet (possibly created on another server)
        // attempt to retrieve permissions from database
        if (!_users.ContainsKey(userId))
        {
            var dataService = ServiceProvider.GetRequiredService<IDataService>();
            await using var db = dataService.GetDataRepository().CreateNewDbContext();
            await CacheUsers(db, userId);
            if (!_users.ContainsKey(userId))
            {
                return [];
            }
            await CacheUserRoles(db, userId);
            await CacheUserTeams(db, userId);
        }
        
        // If the user is found but still doesn't have any roles
        // and was created in the last 5 seconds, wait 3 seconds and re-attempt to retrieve permissions
        // The server creating the user may not have completed assigning permissions to the user yet
        bool hasRoles = _users.ContainsKey(userId) && _userRoles[userId].Count != 0;
        bool hasTeams = _users.ContainsKey(userId) && _userTeams[userId].Count != 0;
        if (!hasRoles && !hasTeams && DateTime.UtcNow.AddSeconds(-5) < _users[userId].CreatedDate)
        {
            await Task.Delay(3000);
            var dataService = ServiceProvider.GetRequiredService<IDataService>();
            await using var db = dataService.GetDataRepository().CreateNewDbContext();
            await CacheUserRoles(db, userId);
            await CacheUserTeams(db, userId);
        }
        
        // Check direct user role permissions
        if (_userRoles.TryGetValue(userId, out var userRoleIds))
        {
            foreach (var roleId in userRoleIds)
            {
                if (!_rolePermissions.TryGetValue(roleId, out var rolePermissions))
                {
                    continue;
                }
                if (permissionNames != null)
                {
                    foreach (var permissionName in permissionNames)
                    {
                        if (rolePermissions.Contains(permissionName))
                        {
                            permissions.Add(permissionName);
                        }
                    }
                }
                else
                {
                    foreach (var permission in rolePermissions)
                    {
                        permissions.Add(permission);
                    }
                }
            }
        }
        
        
        // Check permission acquired through teams
        if (_userTeams.TryGetValue(userId, out var userTeams))
        {
            foreach (var teamId in userTeams)
            {
                if (!_teamRoles.TryGetValue(teamId, out var teamRoleIds))
                {
                    continue;
                }
                foreach (var roleId in teamRoleIds)
                {
                    if (!_rolePermissions.TryGetValue(roleId, out var rolePermissions))
                    {
                        continue;
                    }
                    if (permissionNames != null)
                    {
                        foreach (var permissionName in permissionNames)
                        {
                            if (rolePermissions.Contains(permissionName))
                            {
                                permissions.Add(permissionName);
                            }
                        }
                    }
                    else
                    {
                        foreach (var permission in rolePermissions)
                        {
                            permissions.Add(permission);
                        }
                    }
                }
            }
        }
        
        return permissions.ToArray();
    }

    public static void UpdatePermission(string oldName, string newName)
    {
        foreach (var hashSet in _rolePermissions.Values)
        {
            hashSet.Remove(oldName);
            hashSet.Add(newName);
        }
    }
    public static void RemovePermission(string permissionName)
    {
        foreach (var permissions in _rolePermissions.Values)
        {
            permissions.Remove(permissionName);
        }
    }
    
    public static void RemoveRole(Guid roleId)
    {
        _rolePermissions.TryRemove(roleId, out _);
        foreach (var roles in _userRoles.Values.ToList())
        {
            roles.Remove(roleId);
        }

        foreach (var roles in _teamRoles.Values.ToList())
        {
            roles.Remove(roleId);
        }
    }

    public static void RemoveTeam(Guid teamId)
    {
        _teamRoles.TryRemove(teamId, out _);
        foreach (var teams in _userTeams.Values.ToList())
        {
            teams.Remove(teamId);
        }
    }

    public static void RemoveUser(Guid userId)
    {
        _users.TryRemove(userId, out _);
        _userTeams.TryRemove(userId, out _);
        _userRoles.TryRemove(userId, out _);
    }
    
    public class User
    {
        public DateTime CreatedDate { get; set; }
    }
}