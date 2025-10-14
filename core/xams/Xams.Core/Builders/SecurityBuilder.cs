using System.Linq.Dynamic.Core;
using Xams.Core.Base;
using Xams.Core.Utils;

namespace Xams.Core.Builders;

public class SecurityBuilder
{
    private readonly IXamsDbContext _db;
    private string? _currentRole;
    private readonly List<RolePermissionPair> _rolePermissions = new();

    public SecurityBuilder(IXamsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Sets the current role for subsequent Permission() calls.
    /// Creates the role if it doesn't exist.
    /// </summary>
    /// <param name="roleName">Name of the role</param>
    /// <returns>SecurityBuilder for chaining</returns>
    public SecurityBuilder Role(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));
        }
        _currentRole = roleName;
        return this;
    }

    /// <summary>
    /// Associates a permission with the current role.
    /// Permission must already exist - this method only creates the association.
    /// </summary>
    /// <param name="permissionName">Name of the permission to associate</param>
    /// <returns>SecurityBuilder for chaining</returns>
    public SecurityBuilder Permission(string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));
        }

        if (string.IsNullOrWhiteSpace(_currentRole))
        {
            throw new InvalidOperationException("Must call Role() before Permission()");
        }

        _rolePermissions.Add(new RolePermissionPair
        {
            RoleName = _currentRole,
            PermissionName = permissionName
        });

        return this;
    }

    /// <summary>
    /// Executes the security configuration:
    /// 1. Creates any missing roles
    /// 2. Verifies all permissions exist
    /// 3. Creates role-permission associations that don't already exist
    /// </summary>
    public async Task Execute()
    {
        if (_rolePermissions.Count == 0)
        {
            return; // Nothing to do
        }

        var roleMetadata = Cache.Instance.GetTableMetadata("Role");
        var permissionMetadata = Cache.Instance.GetTableMetadata("Permission");
        var rolePermissionMetadata = Cache.Instance.GetTableMetadata("RolePermission");

        // Get unique role names
        var uniqueRoleNames = _rolePermissions.Select(x => x.RoleName).Distinct().ToList();

        // Step 1: Ensure all roles exist
        foreach (var roleName in uniqueRoleNames)
        {
            var roleDLinq = new DynamicLinq(_db, roleMetadata.Type);
            var roleQuery = roleDLinq.Query.Where("Name == @0", roleName);
            var existingRoles = await roleQuery.ToDynamicListAsync();

            if (!existingRoles.Any())
            {
                // Create the role with deterministic GUID
                var newRole = new Dictionary<string, dynamic>
                {
                    ["RoleId"] = GuidUtil.FromString($"role_{roleName}"),
                    ["Name"] = roleName,
                    ["CreatedDate"] = DateTime.UtcNow
                };

                var entity = EntityUtil.DictionaryToEntity(roleMetadata.Type, newRole);
                _db.Add(entity);
            }
        }

        // Save roles
        await _db.SaveChangesAsync();

        // Step 2: Get all role IDs
        var roleNameToIdMap = new Dictionary<string, Guid>();
        foreach (var roleName in uniqueRoleNames)
        {
            var roleDLinq = new DynamicLinq(_db, roleMetadata.Type);
            var roleQuery = roleDLinq.Query.Where("Name == @0", roleName);
            var roles = await roleQuery.ToDynamicListAsync();
            var role = roles.FirstOrDefault();

            if (role == null)
            {
                throw new Exception($"Failed to find or create role: {roleName}");
            }

            roleNameToIdMap[roleName] = (Guid)role.RoleId;
        }

        // Step 3: Verify all permissions exist
        var uniquePermissionNames = _rolePermissions.Select(x => x.PermissionName).Distinct().ToList();
        var permissionNameToIdMap = new Dictionary<string, Guid>();

        foreach (var permissionName in uniquePermissionNames)
        {
            var permissionDLinq = new DynamicLinq(_db, permissionMetadata.Type);
            var permissionQuery = permissionDLinq.Query.Where("Name == @0", permissionName);
            var permissions = await permissionQuery.ToDynamicListAsync();
            var permission = permissions.FirstOrDefault();

            if (permission == null)
            {
                throw new Exception($"Permission '{permissionName}' does not exist. Permissions must be created before being assigned to roles.");
            }

            permissionNameToIdMap[permissionName] = (Guid)permission.PermissionId;
        }

        // Step 4: Create RolePermission associations (skip duplicates)
        foreach (var pair in _rolePermissions)
        {
            var roleId = roleNameToIdMap[pair.RoleName];
            var permissionId = permissionNameToIdMap[pair.PermissionName];

            // Check if association already exists
            var rpDLinq = new DynamicLinq(_db, rolePermissionMetadata.Type);
            var rpQuery = rpDLinq.Query.Where("RoleId == @0 && PermissionId == @1", roleId, permissionId);
            var existingRolePermissions = await rpQuery.ToDynamicListAsync();

            if (!existingRolePermissions.Any())
            {
                // Create the role-permission association with deterministic GUID
                var newRolePermission = new Dictionary<string, dynamic>
                {
                    ["RolePermissionId"] = GuidUtil.FromString($"{roleId}{permissionId}"),
                    ["RoleId"] = roleId,
                    ["PermissionId"] = permissionId,
                    ["CreatedDate"] = DateTime.UtcNow
                };

                var entity = EntityUtil.DictionaryToEntity(rolePermissionMetadata.Type, newRolePermission);
                _db.Add(entity);
            }
        }

        // Save all role-permission associations
        await _db.SaveChangesAsync();
    }

    private class RolePermissionPair
    {
        public required string RoleName { get; set; }
        public required string PermissionName { get; set; }
    }
}
