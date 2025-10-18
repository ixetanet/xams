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
    /// 1. Validates table permissions (no duplicate table+operation per role)
    /// 2. Creates any missing roles
    /// 3. Verifies all permissions exist
    /// 4. Creates role-permission associations that don't already exist
    /// </summary>
    public async Task Execute()
    {
        if (_rolePermissions.Count == 0)
        {
            return; // Nothing to do
        }

        // Validate table permissions before any database operations
        ValidateTablePermissions();

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

        // Step 4: Create or update RolePermission associations
        foreach (var pair in _rolePermissions)
        {
            var roleId = roleNameToIdMap[pair.RoleName];
            var permissionId = permissionNameToIdMap[pair.PermissionName];

            // Check if this exact association already exists
            var rpDLinq = new DynamicLinq(_db, rolePermissionMetadata.Type);
            var rpQuery = rpDLinq.Query.Where("RoleId == @0 && PermissionId == @1", roleId, permissionId);
            var existingRolePermissions = await rpQuery.ToDynamicListAsync();

            if (existingRolePermissions.Any())
            {
                continue; // Association already exists, skip
            }

            // For TABLE_ permissions, check if role has a different level for same table+operation
            // If conflict found and updated, skip creating new association
            if (await TryUpdateConflictingTablePermission(pair, roleId, permissionId, rolePermissionMetadata, permissionMetadata))
            {
                continue;
            }

            // No conflict found, create new association
            var newRolePermission = new Dictionary<string, dynamic>
            {
                ["RolePermissionId"] = Guid.NewGuid(),
                ["RoleId"] = roleId,
                ["PermissionId"] = permissionId
            };

            var entity = EntityUtil.DictionaryToEntity(rolePermissionMetadata.Type, newRolePermission);
            _db.Add(entity);
        }

        // Save all role-permission associations
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Validates that no role has multiple permissions for the same table+operation combination.
    /// For example, a role cannot have both TABLE_Account_READ_TEAM and TABLE_Account_READ_SYSTEM.
    /// </summary>
    private void ValidateTablePermissions()
    {
        // Filter to only TABLE_ permissions
        var tablePermissions = _rolePermissions
            .Where(rp => rp.PermissionName.StartsWith("TABLE_"))
            .ToList();

        if (tablePermissions.Count == 0)
        {
            return; // No table permissions to validate
        }

        // Parse and group by role + table + operation
        var grouped = tablePermissions
            .Select(rp =>
            {
                var parts = rp.PermissionName.Split('_');
                // Format: TABLE_{TableName}_{Operation}_{Level} or TABLE_{TableName}_{Operation}
                // parts[0] = "TABLE"
                // parts[1] = TableName
                // parts[2] = Operation
                // parts[3] = Level (optional - not present for IMPORT/EXPORT)
                return new
                {
                    rp.RoleName,
                    TableName = parts.Length > 1 ? parts[1] : "",
                    Operation = parts.Length > 2 ? parts[2] : "",
                    rp.PermissionName
                };
            })
            .GroupBy(x => new { x.RoleName, x.TableName, x.Operation })
            .Where(g => g.Count() > 1)
            .ToList();

        if (grouped.Any())
        {
            // Build error message with details about conflicts
            var conflicts = grouped.Select(g =>
            {
                var permissions = string.Join("\n    - ", g.Select(x => x.PermissionName));
                return $"Role '{g.Key.RoleName}' has multiple permissions for table '{g.Key.TableName}' operation '{g.Key.Operation}':\n    - {permissions}";
            });

            var errorMessage = "A role cannot have multiple permissions for the same table and operation combination.\n\n" +
                              string.Join("\n\n", conflicts) +
                              "\n\nA role can only have one permission per table+operation combination.";

            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Checks if a conflicting table permission exists for the role and updates it if found.
    /// A conflict occurs when the role has a different permission level for the same table+operation.
    /// </summary>
    /// <returns>True if a conflict was found and updated; false otherwise</returns>
    private async Task<bool> TryUpdateConflictingTablePermission(
        RolePermissionPair pair,
        Guid roleId,
        Guid newPermissionId,
        dynamic rolePermissionMetadata,
        dynamic permissionMetadata)
    {
        // Only check TABLE_ permissions for conflicts
        if (!pair.PermissionName.StartsWith("TABLE_"))
        {
            return false;
        }

        var parts = pair.PermissionName.Split('_');
        if (parts.Length < 3)
        {
            return false; // Invalid format, skip
        }

        string tableName = parts[1];
        string operation = parts[2];

        // Query all RolePermissions for this role
        var allRpDLinq = new DynamicLinq(_db, rolePermissionMetadata.Type);
        var allRpQuery = allRpDLinq.Query.Where("RoleId == @0", roleId);
        var rolePermissionsForRole = await allRpQuery.ToDynamicListAsync();

        // Check each existing RolePermission for conflicts
        foreach (var existingRp in rolePermissionsForRole)
        {
            var existingPermId = (Guid)existingRp.PermissionId;

            // Find the permission name for this RolePermission
            var permDLinq = new DynamicLinq(_db, permissionMetadata.Type);
            var permQuery = permDLinq.Query.Where("PermissionId == @0", existingPermId);
            var existingPermissions = await permQuery.ToDynamicListAsync();
            var existingPermission = existingPermissions.FirstOrDefault();

            if (existingPermission == null)
            {
                continue; // Permission not found, skip
            }

            string existingPermName = (string)existingPermission.Name;

            // Check if it's a TABLE_ permission with same table+operation
            if (!existingPermName.StartsWith("TABLE_"))
            {
                continue;
            }

            var existingParts = existingPermName.Split('_');
            if (existingParts.Length < 3)
            {
                continue;
            }

            string existingTable = existingParts[1];
            string existingOperation = existingParts[2];

            // Found conflict: same table+operation, different permission name
            if (existingTable == tableName &&
                existingOperation == operation &&
                existingPermName != pair.PermissionName)
            {
                // Update the existing RolePermission to point to the new permission
                var existingRolePermissionId = (Guid)existingRp.RolePermissionId;
                var existingRoleId = (Guid)existingRp.RoleId;

                var updatedRolePermission = new Dictionary<string, dynamic>
                {
                    ["RolePermissionId"] = existingRolePermissionId,
                    ["RoleId"] = existingRoleId,
                    ["PermissionId"] = newPermissionId
                };

                var entityToUpdate = EntityUtil.DictionaryToEntity(rolePermissionMetadata.Type, updatedRolePermission);
                _db.Update(entityToUpdate);

                return true; // Conflict found and updated
            }
        }

        return false; // No conflict found
    }

    private class RolePermissionPair
    {
        public required string RoleName { get; set; }
        public required string PermissionName { get; set; }
    }
}
