using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Interfaces;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core
{
    public static class Permissions
    {
        public static string CacheLastUpdate = string.Empty;
        public static Dictionary<Guid, HashSet<string>> CachedPermissions { get; set; } = new();

        /// <summary>
        /// Returns a list of permissions for a given user.
        /// </summary>
        /// <param name="dataContext"></param>
        /// <param name="userId"></param>
        /// <param name="permissionNames"></param>
        /// <returns></returns>
        public static async Task<string[]> GetUserPermissions(BaseDbContext dataContext, Guid userId,
            string[]? permissionNames = null)
        {
            // Check if Cache should be used first
            if (CachedPermissions.Any())
            {
                // If the permissions for this user have been cached then use what's in the cache
                // Otherwise query the database for the user's permissions
                if (CachedPermissions.TryGetValue(userId, out var permission))
                {
                    if (permissionNames == null)
                    {
                        return permission.ToArray();
                    }

                    List<string> results = new();
                    foreach (var permissionName in permissionNames)
                    {
                        if (CachedPermissions[userId].Contains(permissionName))
                        {
                            results.Add(permissionName);
                        }
                    }

                    return results.ToArray();
                }
            }
            
            var securityServicePermissions = await ExecuteSecurityServices(userId, dataContext, permissionNames);
            var permissionContextType = Cache.Instance.GetTableMetadata("Permission");
            var rolePermissionContextType = Cache.Instance.GetTableMetadata("RolePermission");
            var teamRoleContextType = Cache.Instance.GetTableMetadata("TeamRole");
            var teamUserContextType = Cache.Instance.GetTableMetadata("TeamUser");
            var userRoleContextType = Cache.Instance.GetTableMetadata("UserRole");

            IQueryable permissionQuery = new DynamicLinq<BaseDbContext>(dataContext, permissionContextType.Type).Query;
            IQueryable rolePermissionQuery =
                new DynamicLinq<BaseDbContext>(dataContext, rolePermissionContextType.Type).Query;
            IQueryable teamRoleQuery = new DynamicLinq<BaseDbContext>(dataContext, teamRoleContextType.Type).Query;
            IQueryable teamUserQuery = new DynamicLinq<BaseDbContext>(dataContext, teamUserContextType.Type).Query;
            IQueryable userRoleQuery = new DynamicLinq<BaseDbContext>(dataContext, userRoleContextType.Type).Query;

            string permissionOr = string.Empty;
            if (permissionNames != null)
            {
                permissionOr = string.Join(" || ", permissionNames.Select(x => $"Permission.Name == \"{x}\""));
            }


            IQueryable teamPermissions = permissionQuery
                .Join(rolePermissionQuery, "PermissionId", "PermissionId",
                    "new(outer as Permission, inner as RolePermission)")
                .Join(teamRoleQuery, "RolePermission.RoleId", "RoleId",
                    "new(outer.Permission, outer.RolePermission, inner as TeamRole)")
                .Join(teamUserQuery, "TeamRole.TeamId", "TeamId",
                    "new(outer.Permission, outer.RolePermission, outer.TeamRole, inner as TeamUser)")
                .Where($"TeamUser.UserId == @0", userId);
            if (permissionNames != null)
            {
                teamPermissions = teamPermissions.Where(permissionOr);
            }

            teamPermissions = teamPermissions.Select("Permission.Name").Distinct();

            IQueryable userPermissions = permissionQuery
                .Join(rolePermissionQuery, "PermissionId", "PermissionId",
                    "new(outer as Permission, inner as RolePermission)")
                .Join(userRoleQuery, "RolePermission.RoleId", "RoleId",
                    "new(outer.Permission, outer.RolePermission, inner as UserRole)")
                .Where("UserRole.UserId == @0", userId);

            if (permissionNames != null)
            {
                userPermissions = userPermissions.Where(permissionOr);
            }

            userPermissions = userPermissions.Select("Permission.Name").Distinct();

            teamPermissions = teamPermissions.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Union",
                    [teamPermissions.ElementType],
                    teamPermissions.Expression,
                    userPermissions.Expression
                ));

            securityServicePermissions.AddRange(teamPermissions.ToDynamicList<string>());
            return securityServicePermissions.Distinct().ToArray();
        }

        private static void GetUserPermissionsQuery()
        {
            
        }

        public enum PermissionLevel
        {
            System,
            Team,
            User
        }

        public static PermissionLevel? GetHighestPermission(string[]? permissions)
        {
            if (permissions == null)
            {
                return null;
            }

            var system = permissions.FirstOrDefault(x => x.EndsWith("_SYSTEM"));
            if (system != null)
            {
                return PermissionLevel.System;
            }

            var team = permissions.FirstOrDefault(x => x.EndsWith("_TEAM"));
            if (team != null)
            {
                return PermissionLevel.Team;
            }

            var user = permissions.FirstOrDefault(x => x.EndsWith("_USER"));
            if (user != null)
            {
                return PermissionLevel.User;
            }

            return null;
        }

        public static async Task<string[]> GetUserTablePermissions(BaseDbContext dataContext, Guid userId,
            string tableName,
            string[] operations)
        {
            List<string> tablePermissions = new();
            foreach (var operation in operations)
            {
                tablePermissions.Add($"TABLE_{tableName}_{operation}_USER");
                tablePermissions.Add($"TABLE_{tableName}_{operation}_TEAM");
                tablePermissions.Add($"TABLE_{tableName}_{operation}_SYSTEM");
            }
            
            string[] permissions = await GetUserPermissions(dataContext, userId, tablePermissions.ToArray());
            return permissions;
        }

        public static async Task<string[]> GetUserTablePermissions(BaseDbContext dataContext, Guid userId,
            string[] tableNames, string operation)
        {
            List<string> tablePermission = new();
            foreach (var tableName in tableNames)
            {
                tablePermission.AddRange(new[]
                {
                    $"TABLE_{tableName}_{operation}_USER",
                    $"TABLE_{tableName}_{operation}_TEAM",
                    $"TABLE_{tableName}_{operation}_SYSTEM"
                });
            }

            string[] permissions = await GetUserPermissions(dataContext, userId, tablePermission.ToArray());
            return permissions;
        }

        public static async Task<List<string>> ExecuteSecurityServices(Guid userId, BaseDbContext dataContext,
            string[]? permissionNames)
        {
            // If no permission names are provided, get all permissions
            if (permissionNames == null)
            {
                var permissionContextType = Cache.Instance.GetTableMetadata("Permission");
                var permissionQuery = new DynamicLinq<BaseDbContext>(dataContext, permissionContextType.Type).Query;
                permissionNames = (await permissionQuery.Select("Name").ToDynamicListAsync<string>()).ToArray();
            }

            List<PermissionRequest> permissionRequests = new();
            foreach (var permissionName in permissionNames)
            {
                PermissionType permissionType = PermissionType.Other;
                string actionName = string.Empty;
                string tableName = string.Empty;
                string jobName = string.Empty;
                if (permissionName.StartsWith("ACTION_"))
                {
                    permissionType = PermissionType.Action;
                    actionName = permissionName.Substring(7);
                }

                if (permissionName.StartsWith("TABLE_"))
                {
                    var parts = permissionName.Split('_');

                    List<string> tableNameParts = new();
                    if (parts[^1] == "IMPORT" || parts[^1] == "EXPORT")
                    {
                        Enum.TryParse(parts[^1], true, out permissionType);

                        for (int i = 1; i < parts.Length - 1; i++)
                        {
                            tableNameParts.Add(parts[i]);
                        }

                        tableName = string.Join("_", tableNameParts);
                    }
                    else
                    {
                        Enum.TryParse(parts[^2], true, out permissionType);
                        for (int i = 1; i < parts.Length - 2; i++)
                        {
                            tableNameParts.Add(parts[i]);
                        }

                        tableName = string.Join("_", tableNameParts);
                    }
                }

                if (permissionName.StartsWith("JOB_"))
                {
                    permissionType = PermissionType.Job;
                    jobName = permissionName.Substring(4);
                }

                permissionRequests.Add(new PermissionRequest(permissionType, permissionName, tableName, actionName,
                    jobName));
            }

            foreach (var serviceSecurityInfo in Cache.Instance.ServiceSecurityInfos)
            {
                if (serviceSecurityInfo.Type == null)
                {
                    continue;
                }

                var securityService = Activator.CreateInstance(serviceSecurityInfo.Type);

                if (securityService == null)
                {
                    continue;
                }
                
                var securityContext = new SecurityContext(userId, dataContext, permissionRequests);

                var response = await ((IServiceSecurity)securityService).Execute(securityContext);
                if (!response.Succeeded)
                {
                    throw new Exception(response.FriendlyMessage);
                }

                List<string> approvedPermissions = new();
                foreach (var permissionRequest in permissionRequests)
                {
                    if (permissionRequest.Approved)
                    {
                        approvedPermissions.Add(permissionRequest.PermissionName);
                    }
                }

                return approvedPermissions;
            }

            return new List<string>();
        }

        
        internal static List<TablePermission> ToTablePermissions(this string[] permissions)
        {
            List<TablePermission> tablePermissions = new();
            Regex crudRegex = new Regex("TABLE_([A-Za-z0-9_]+)_(UPDATE|READ|DELETE|CREATE|ASSIGN)_(USER|TEAM|SYSTEM)");
            Regex importExportRegex = new Regex("TABLE_([A-Za-z0-9_]+)_(IMPORT|EXPORT)");
            foreach (var permission in permissions)
            {
                var match = crudRegex.Match(permission);
                if (!match.Success)
                {
                    match = importExportRegex.Match(permission);
                }

                if (!match.Success)
                {
                    continue;
                }

                string tableName = match.Groups[1].Value;
                TablePermission? tablePermission = tablePermissions.FirstOrDefault(x => x.Table == tableName);
                if (tablePermission == null)
                {
                    tablePermission = new TablePermission()
                    {
                        Table = tableName,
                        Permissions = [permission]
                    };
                    tablePermissions.Add(tablePermission);
                }
                else
                {
                    tablePermission.Permissions.Add(permission);
                }
            }
            return tablePermissions;
        }

        public static async Task RefreshCache(BaseDbContext dataContext, ILogger logger)
        {
            // Allow up to 5 minutes for the cache to refresh
            dataContext.Database.SetCommandTimeout(new TimeSpan(0, 5, 0));
            // Is Permission Cache enabled?
            var settingMetadta = Cache.Instance.GetTableMetadata("Setting");
            DynamicLinq<BaseDbContext> settingDynamicLinq = new DynamicLinq<BaseDbContext>(dataContext, settingMetadta.Type);
            IQueryable settingQuery = settingDynamicLinq.Query;
            settingQuery = settingQuery.Where("Name == @0", SystemRecords.CachePermissionsSetting);
            var settingRecords = await settingQuery.ToDynamicListAsync();
            if (settingRecords.Count == 0)
            {
                throw new Exception("Failed to find the Cache Permissions Setting record.");
            }

            var settingRecord = settingRecords.First();
        
            if (bool.TryParse(settingRecord.Value, out bool isEnabled) && !isEnabled)
            {
                CachedPermissions.Clear();
                CacheLastUpdate = string.Empty;
                return;
            }
            
            // Have the Permissions been updated since the last time the cache was refreshed?
            var systemMetadata = Cache.Instance.GetTableMetadata("System");
            DynamicLinq<BaseDbContext> systemDynamicLinq = new DynamicLinq<BaseDbContext>(dataContext, systemMetadata.Type);
            IQueryable query = systemDynamicLinq.Query;
            query = query.Where("Name == @0", SystemRecords.CachePermissionsLastUpdateSystem);
            var systemRecords = await query.ToDynamicListAsync();
            if (systemRecords.Count == 0)
            {
                throw new Exception("Failed to find the Cache Permissions Last Update System record.");
            }

            var systemRecord = (object)systemRecords.First();

            // Cached Permissions are latest
            if (systemRecord.GetValue<string>("Value") == CacheLastUpdate)
            {
                return;
            }
            
            logger.LogInformation("Refreshing Permission Cache");
            
            var permissionContextType = Cache.Instance.GetTableMetadata("Permission");
            var rolePermissionContextType = Cache.Instance.GetTableMetadata("RolePermission");
            var teamRoleContextType = Cache.Instance.GetTableMetadata("TeamRole");
            var teamUserContextType = Cache.Instance.GetTableMetadata("TeamUser");
            var userRoleContextType = Cache.Instance.GetTableMetadata("UserRole");

            IQueryable permissionQuery = new DynamicLinq<BaseDbContext>(dataContext, permissionContextType.Type).Query;
            IQueryable rolePermissionQuery =
                new DynamicLinq<BaseDbContext>(dataContext, rolePermissionContextType.Type).Query;
            IQueryable teamRoleQuery = new DynamicLinq<BaseDbContext>(dataContext, teamRoleContextType.Type).Query;
            IQueryable teamUserQuery = new DynamicLinq<BaseDbContext>(dataContext, teamUserContextType.Type).Query;
            IQueryable userRoleQuery = new DynamicLinq<BaseDbContext>(dataContext, userRoleContextType.Type).Query;
            
            IQueryable teamPermissions = permissionQuery
                .Join(rolePermissionQuery, "PermissionId", "PermissionId",
                    "new(outer as Permission, inner as RolePermission)")
                .Join(teamRoleQuery, "RolePermission.RoleId", "RoleId",
                    "new(outer.Permission, outer.RolePermission, inner as TeamRole)")
                .Join(teamUserQuery, "TeamRole.TeamId", "TeamId",
                    "new(outer.Permission, outer.RolePermission, outer.TeamRole, inner as TeamUser)");

            teamPermissions = teamPermissions.Select("new (Permission.Name as PermissionName, TeamUser.UserId as UserId)").Distinct();

            IQueryable userPermissions = permissionQuery
                .Join(rolePermissionQuery, "PermissionId", "PermissionId",
                    "new(outer as Permission, inner as RolePermission)")
                .Join(userRoleQuery, "RolePermission.RoleId", "RoleId",
                    "new(outer.Permission, outer.RolePermission, inner as UserRole)");
                

            userPermissions = userPermissions.Select("new (Permission.Name as PermissionName, UserRole.UserId as UserId)").Distinct();

            teamPermissions = teamPermissions.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Union",
                    [teamPermissions.ElementType],
                    teamPermissions.Expression,
                    userPermissions.Expression
                ));

            var results = await teamPermissions.ToDynamicListAsync();
            
            // Get distinct user ids
            Dictionary<Guid,HashSet<string>> cachedPermissions = new();
            
            var userIds = results.Select(x => (Guid)x.UserId).Distinct().ToArray();

            int totalPermissions = 0;
            foreach (var userId in userIds)
            {
                var permissions = results.Where(x => x.UserId == userId).ToList();
                
                HashSet<string> userPermissionsHash = new();
                foreach (var permission in permissions)
                {
                    userPermissionsHash.Add(permission.PermissionName);
                    totalPermissions++;
                }
                cachedPermissions[userId] = userPermissionsHash;
            }
            
            CachedPermissions = cachedPermissions;
            CacheLastUpdate = systemRecord.GetValue<string>("Value");
            logger.LogInformation("Cached {TotalPermissions} Permissions", totalPermissions);
        }

        public class PermissionRequest
        {
            public PermissionType Type { get; private set; }
            public string PermissionName { get; private set; }
            public string TableName { get; private set; }
            public string ActionName { get; private set; }
            public string JobName { get; private set; }
            public bool Approved { get; set; } = false;

            public PermissionRequest(PermissionType type, string permissionName, string tableName, string actionName,
                string jobName)
            {
                Type = type;
                PermissionName = permissionName;
                TableName = tableName;
                ActionName = actionName;
                JobName = jobName;
            }
        }
    }
}

public class TablePermission
{
    public required string Table { get; set; }
    public required List<string> Permissions { get; set; }
}

public enum PermissionType
{
    Read,
    Create,
    Update,
    Delete,
    Assign,
    Action,
    Job,
    Import,
    Export,
    Other
}