using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Reflection;
using DocumentFormat.OpenXml.Wordprocessing;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Startup
{
    public class SystemRecords
    {
        private readonly IDataService _dataService;
        public static readonly Guid SystemUserId = new("f8a43b04-4752-4fda-a89f-62bebcd8240c");
        public static readonly Guid SystemAdministratorRoleId = new("64589861-0481-4dbb-a96f-9b8b6546c40d");
        public static readonly Guid SystemAdministratorTeamId = new("64639b10-d93c-4a2c-8d8c-7bc799c3feae");
        public static readonly Guid SystemUserRoleId = new("58e1362d-b7cb-4e32-96e5-ccf86b67d28d");
        public static readonly Guid SystemAdministratorsTeamRoleId = new("fd74cc60-ac25-41b4-862c-1621cf03282d");
        public static readonly Guid SystemTeamUserId = new("6c952b65-6530-4248-b6f6-d0f9a2305c6f");
        public static readonly string CachePermissionsSetting = "CACHE_PERMISSIONS";
        public static readonly string CachePermissionsLastUpdateSystem = "CACHE_PERMISSIONS_LAST_UPDATE";
    
        public SystemRecords(IDataService dataService)
        {
            _dataService = dataService;
        }
        // Ensure system user exists
        public async Task CreateSystemUser()
        {
            Console.WriteLine("Creating System User");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("User");
        
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("UserId == @0", SystemUserId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemUser = new();
                systemUser["UserId"] = SystemUserId;
                systemUser["Name"] = "SYSTEM";
                systemUser["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemUser);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
            
        // Ensure system roles exist
        public async Task CreateSystemRoles()
        {
            Console.WriteLine("Creating System Roles");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("Role");
        
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("RoleId == @0", SystemAdministratorRoleId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemRole = new();
                systemRole["RoleId"] = SystemAdministratorRoleId;
                systemRole["Name"] = "System Administrator";
                systemRole["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemRole);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
            
        // Ensure system teams exist
        public async Task CreateSystemTeams()
        {
            Console.WriteLine("Creating System Teams");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("Team");
        
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("TeamId == @0", SystemAdministratorTeamId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemTeam = new();
                systemTeam["TeamId"] = SystemAdministratorTeamId;
                systemTeam["Name"] = "System Administrators";
                systemTeam["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemTeam);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
            
        // Ensure system permissions exist
        public async Task CreateSystemPermissions()
        {
            Console.WriteLine("Creating System Permissions");
            
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var permissionMetadata = Cache.Instance.GetTableMetadata("Permission");
            PropertyInfo nameProp = permissionMetadata.Type.GetProperty("Name") ?? throw new ArgumentNullException($"baseDbContextType.Type.GetProperty(\"Name\")");
        
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, permissionMetadata.Type);
            IQueryable existingPermissionsQuery = dynamicLinq.Query;
            existingPermissionsQuery = existingPermissionsQuery.Where("Tag == @0", "System");
            List<dynamic> existingPermissions = await existingPermissionsQuery.ToDynamicListAsync();
            List<string> permissionNames = new();
            List<string> actualPermissions = new();
            
            // Execute Service Permissions - Permissions that are created by services
            permissionNames.AddRange(await ExecuteServicePermission());

            // Create Action Permissions
            foreach (var serviceAction in Cache.Instance.ServiceActions)
            {
                var serviceActionInfo = serviceAction.Value;
                permissionNames.Add($"ACTION_{serviceActionInfo.ServiceActionAttribute.Name}");
            }
            
            // Create Table Permissions
            var tableMetadata = Cache.Instance.GetTableMetadata();
            var permissionOperations = new[]
            {
                "CREATE",
                "READ",
                "UPDATE",
                "DELETE",
                "ASSIGN",
            };
            
            var permissionLevels = new[]
            {
                "SYSTEM",
                "TEAM",
                "USER"
            };
            
            foreach (var metadata in tableMetadata)
            {
                if (metadata.TableAttribute == null)
                {
                    throw new Exception($"{metadata.Type.Name} is missing a TableAttribute. This should match the name of the class.");
                }
                foreach (var permissionOperation in permissionOperations)
                {
                    foreach (var permissionLevel in permissionLevels)
                    {
                        var permissionName = $"TABLE_{metadata.TableAttribute?.Name}_{permissionOperation}_{permissionLevel}";
                        permissionNames.Add(permissionName);
                    }
                }
            }
            
            // Create Table Import Permissions
            foreach (var metadata in tableMetadata)
            {
                permissionNames.Add($"TABLE_{metadata.TableAttribute.Name}_IMPORT");
                permissionNames.Add($"TABLE_{metadata.TableAttribute.Name}_EXPORT");
            }
        
            // Other Permissions
            permissionNames.Add($"ACCESS_ADMIN_DASHBOARD");
            
            foreach (var permissionName in permissionNames)
            {
                var permission = existingPermissions.FirstOrDefault(p => nameProp.GetValue(p).ToString() == permissionName);
                actualPermissions.Add(permissionName);
                if (permission == null)
                {
                    Dictionary<string, dynamic> systemPermission = new();
                    // Permissions created on startup should have predictable Id's
                    // in case of being exported\imported to another system
                    systemPermission["PermissionId"] = GuidUtil.FromString($"permission_{permissionName}");
                    systemPermission["Name"] = permissionName;
                    systemPermission["Tag"] = "System";
                    systemPermission["CreatedDate"] = DateTime.UtcNow;
                    object entity = EntityUtil.DictionaryToEntity(permissionMetadata.Type, systemPermission);
                    baseDbContext.Add(entity);
                }
            }

            // Delete any *system* permissions that no longer exist
            foreach (var existingPermission in existingPermissions)
            {
                var existingPermissionName = (string)nameProp.GetValue(existingPermission).ToString();
                var actualPermissionExists = actualPermissions.Any(m => m == existingPermissionName);
                if (actualPermissionExists == false)
                {
                    // Delete any RolePermissions that reference this permission
                    DynamicLinq<BaseDbContext> dLinq = new DynamicLinq<BaseDbContext>(baseDbContext, Cache.Instance.GetTableMetadata("RolePermission").Type);
                    IQueryable query = dLinq.Query;
                    query = query.Where("PermissionId == @0", (Guid)existingPermission.PermissionId);
                    var rolePermissions = await query.ToDynamicListAsync();
                    baseDbContext.RemoveRange(rolePermissions);
                    baseDbContext.Remove(existingPermission);
                }
            }

            await baseDbContext.SaveChangesAsync();
        }
    
        // Ensure system roles have system permissions
        public async Task CreateRolePermissions()
        {
            Console.WriteLine("Creating Role Permissions");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var rolePermissionMetadata = Cache.Instance.GetTableMetadata("RolePermission");
            var permissionMetadata = Cache.Instance.GetTableMetadata("Permission");
            var rolePermissionPermissionIdProp = rolePermissionMetadata.Type.GetProperty("PermissionId") ?? throw new ArgumentNullException($"rolePermissionMetadata.Type.GetProperty(\"PermissionId\")");
            var permissionPermissionIdProp = permissionMetadata.Type.GetProperty("PermissionId") ?? throw new ArgumentNullException($"permissionMetadata.Type.GetProperty(\"PermissionId\")");
            var permissionNameProp = permissionMetadata.Type.GetProperty("Name") ?? throw new ArgumentNullException($"permissionMetadata.Type.GetProperty(\"Name\")");
        
            // Retrieve all permissions for system administrator role
            DynamicLinq<BaseDbContext> rolPermissionLinq = new DynamicLinq<BaseDbContext>(baseDbContext, rolePermissionMetadata.Type);
            IQueryable query = rolPermissionLinq.Query;
            query = query.Where("RoleId == @0", SystemAdministratorRoleId);
            List<dynamic> rolePermissions = await query.ToDynamicListAsync();
            
            // Retrieve all permissions
            DynamicLinq<BaseDbContext> permissionLinq = new DynamicLinq<BaseDbContext>(baseDbContext, permissionMetadata.Type);
            IQueryable permissionQuery = permissionLinq.Query;
            List<dynamic> permissions = await permissionQuery.ToDynamicListAsync();
            
            foreach (var permission in permissions)
            {
                string permissionName = (String)permissionNameProp.GetValue(permission);
                string[] otherPermissions = { "ACCESS_ADMIN_DASHBOARD" };
                if (!(permissionName.StartsWith("ACTION_") || 
                      permissionName.StartsWith("JOB_") || 
                      permissionName.EndsWith("_SYSTEM") || 
                      permissionName.EndsWith("_IMPORT") ||
                      permissionName.EndsWith("_EXPORT") ||
                      otherPermissions.Contains(permissionName)))
                {
                    continue;
                }
                
                Guid permissionId = (Guid)permissionPermissionIdProp.GetValue(permission);
                var rolePermission = rolePermissions.FirstOrDefault(rp => (Guid)rolePermissionPermissionIdProp.GetValue(rp) == permissionId);
                if (rolePermission == null)
                {
                    Dictionary<string, dynamic> systemRolePermission = new();
                    // Initial system generated Id's should be consistent across systems to prevent duplicates
                    systemRolePermission["RolePermissionId"] = GuidUtil.FromString($"{SystemAdministratorRoleId}{permissionId}");
                    systemRolePermission["RoleId"] = SystemAdministratorRoleId;
                    systemRolePermission["PermissionId"] = permissionId;
                    systemRolePermission["CreatedDate"] = DateTime.UtcNow;
                    object entity = EntityUtil.DictionaryToEntity(rolePermissionMetadata.Type, systemRolePermission);
                    baseDbContext.Add(entity);
                }
            }
        
            await baseDbContext.SaveChangesAsync();
        }
            
        // Ensure system user has system roles
        public async Task CreateUserRoles()
        {
            Console.WriteLine("Creating User Roles");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("UserRole");
        
            // Create system administrator role for system user
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("UserRoleId == @0", SystemUserRoleId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemUserRole = new();
                systemUserRole["UserRoleId"] = SystemUserRoleId;
                systemUserRole["UserId"] = SystemUserId;
                systemUserRole["RoleId"] = SystemAdministratorRoleId;
                systemUserRole["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemUserRole);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
    
        // Ensure system teams have system roles
        public async Task CreateTeamRoles()
        {
            Console.WriteLine("Creating Team Roles");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("TeamRole");
        
            // Create system administrator role for system user
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("TeamRoleId == @0", SystemAdministratorsTeamRoleId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemTeamRole = new();
                systemTeamRole["TeamRoleId"] = SystemAdministratorsTeamRoleId;
                systemTeamRole["TeamId"] = SystemAdministratorTeamId;
                systemTeamRole["RoleId"] = SystemAdministratorRoleId;
                systemTeamRole["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemTeamRole);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
        
        // Ensure system user has system teams
        public async Task CreateTeamUsers()
        {
            Console.WriteLine("Creating Team Users");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var baseDbContextType = Cache.Instance.GetTableMetadata("TeamUser");
        
            // Create system administrator role for system user
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, baseDbContextType.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where("TeamUserId == @0", SystemTeamUserId);
            if (query.ToDynamicList().Count == 0)
            {
                Dictionary<string, dynamic> systemTeamUser = new();
                systemTeamUser["TeamUserId"] = SystemTeamUserId;
                systemTeamUser["TeamId"] = SystemAdministratorTeamId;
                systemTeamUser["UserId"] = SystemUserId;
                systemTeamUser["CreatedDate"] = DateTime.UtcNow;
                object entity = EntityUtil.DictionaryToEntity(baseDbContextType.Type, systemTeamUser);
                baseDbContext.Add(entity);
                await baseDbContext.SaveChangesAsync();
            }
        }
        
        // Ensure system records are created for the system and setting table
        public async Task CreateSettingAndSystemRecords()
        {
            Console.WriteLine("Creating System and Setting Records");
            BaseDbContext baseDbContext = _dataService.GetDataRepository().CreateNewDbContext();
            var settingMetadata = Cache.Instance.GetTableMetadata("Setting");
            var systemMetadata = Cache.Instance.GetTableMetadata("System");
            

            var settings = new[]
            {
                new
                {
                    Name = CachePermissionsSetting,
                    Value = "false",
                }
            };

            var systems = new[]
            {
                new
                {
                    Name = CachePermissionsLastUpdateSystem,
                    Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                }
            };

            foreach (var setting in settings)
            {
                Guid id = GuidUtil.FromString(setting.Name);
                DynamicLinq<BaseDbContext>
                    dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, settingMetadata.Type);
                IQueryable query = dynamicLinq.Query;
                query = query.Where("SettingId == @0", id);
                if (query.ToDynamicList().Count == 0)
                {
                    Dictionary<string, dynamic> systemSetting = new();
                    systemSetting["SettingId"] = id;
                    systemSetting["Name"] = setting.Name;
                    systemSetting["Value"] = setting.Value;
                    object entity = EntityUtil.DictionaryToEntity(settingMetadata.Type, systemSetting);
                    baseDbContext.Add(entity);
                }
            }

            foreach (var system in systems)
            {
                Guid id = GuidUtil.FromString(system.Name);
                DynamicLinq<BaseDbContext>
                    dynamicLinq = new DynamicLinq<BaseDbContext>(baseDbContext, systemMetadata.Type);
                IQueryable query = dynamicLinq.Query;
                query = query.Where("SystemId == @0", id);
                if (query.ToDynamicList().Count == 0)
                {
                    Dictionary<string, dynamic> systemSetting = new();
                    systemSetting["SystemId"] = id;
                    systemSetting["Name"] = system.Name;
                    systemSetting["Value"] = system.Value;
                    object entity = EntityUtil.DictionaryToEntity(systemMetadata.Type, systemSetting);
                    baseDbContext.Add(entity);
                }
            }

            await baseDbContext.SaveChangesAsync();
        }

        
        private async Task<List<string>> ExecuteServicePermission()
        {
            List<string> permissionNames = new();
            foreach (var servicePermissionInfo in Cache.Instance.ServicePermissionInfos)
            {
                var servicePermission = Activator.CreateInstance(servicePermissionInfo.Type);
                
                if (servicePermission == null)
                {
                    continue;
                }
                
                var permissionContext = new PermissionContext(_dataService);
                
                var response = await ((IServicePermission)servicePermission).Execute(permissionContext);
                
                if (!response.Succeeded)
                {
                    throw new Exception(response.FriendlyMessage);
                }

                if (response.Data == null)
                {
                    continue;
                }
                
                permissionNames.AddRange(response.Data);
            }

            return permissionNames;
        }
        
    }
}