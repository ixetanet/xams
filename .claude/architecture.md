# Xams Architecture Deep Dive & Cache System

← [Back to Main Documentation](../CLAUDE.md)

## Table of Contents

1. [Pipeline Architecture](#pipeline-architecture)
2. [Service Layer Architecture](#service-layer-architecture)
3. [Security Architecture](#security-architecture)
4. [Transaction Management](#transaction-management)
5. [Cache System](#cache-system)

---

## Pipeline Architecture

The pipeline implements the Chain of Responsibility pattern for all CRUD operations:

```
Request → PreValidation Logic → Permission Check → PreOperation Logic → Save → PostOperation Logic → Response
```

### Pipeline Stages

1. **PreValidation Logic** (`LogicStage.PreValidation`)

   - Executes AFTER initial validation but BEFORE permission checks
   - Used for data transformation or early validation
   - No database changes should occur here

2. **Permission Check** (Automatic via `PipePermissionRules`)

   - Validates user permissions based on operation
   - Checks record ownership (User/Team/System levels)
   - Returns 400 with permission error if unauthorized

3. **PreOperation Stage** (`LogicStage.PreOperation`)

   - Executes after security validation
   - Most common stage for business logic
   - Changes made here will be saved
   - Access to both Entity and PreEntity (for updates)

4. **Database Save** (Automatic)

   - Entity Framework SaveChanges()
   - Triggers only when needed or PostOperation exists
   - All changes within single transaction

5. **PostOperation Stage** (`LogicStage.PostOperation`)
   - Executes after database save
   - Primary key is available for new records
   - Used for dependent record creation
   - Still within transaction boundary

### Pipeline Context Flow

The `ServiceContext` flows through all stages providing:

```csharp
context.ExecutingUserId     // Current user ID
context.DataOperation       // Create/Read/Update/Delete
context.LogicStage         // Current pipeline stage
context.Entity             // Current entity state
context.PreEntity          // Original entity (updates only)
context.GetDbContext<T>()  // Database context
context.Create/Update/Delete() // CRUD operations
```

---

## Service Layer Architecture

### Service Types

1. **ServiceLogic** - CRUD operation hooks

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Create | DataOperation.Update, LogicStage.PreOperation, 100)]
public class WidgetService : IServiceLogic
```

2. **ServiceAction** - Custom endpoints

```csharp
[ServiceAction(nameof(ExportWidgets))]
public class ExportWidgets : IServiceAction
```

3. **ServiceJob** - Scheduled tasks

```csharp
[ServiceJob("WidgetCleanup", "Primary-Queue", "00:05:00")]
public class WidgetCleanupJob : IServiceJob
```

4. **ServiceStartup** - Initialization logic

```csharp
[ServiceStartup]
public class InitializeWidgets : IServiceStartup
```

---

## Security Architecture

### Permission Hierarchy

```
User → UserRole → Role → RolePermission → Permission
User → TeamUser → Team → TeamRole → Role
```

### Record Ownership Model

- **BaseEntity** provides `OwningUserId` and `OwningTeamId`
- At least one ownership field must be set
- Permissions check ownership based on level:
  - **User Level**: Can only access own records
  - **Team Level**: Can access team records
  - **System Level**: Can access all records

### Permission Format

```
TABLE_{TableName}_{Operation}_{Level}
Example: TABLE_Widget_CREATE_SYSTEM
```

---

## Transaction Management

- Every API call wrapped in single transaction
- Rollback on any exception or `ServiceResult.Error()`
- `ExecutionId` tracks related operations
- Bulk operations maintain transaction consistency

---

## Cache System

Xams implements a sophisticated two-tier caching system for optimal performance and metadata-driven UI generation.

### Main Cache (`core/xams/Xams.Core/Cache.cs`)

A singleton instance that caches framework metadata and service configurations during startup:

#### Core Cached Data

- **Entity Metadata** - Table names, field types, primary keys, UI attributes from Entity Framework
- **Service Logic** - All service classes with their attributes, execution order, and pipeline stages
- **Actions** - Custom endpoint handlers and their configurations
- **Jobs** - Scheduled tasks, execution schedules, and server assignments
- **Permissions** - Service permission requirements and mappings
- **Audit Configuration** - Which tables and fields are audited
- **Server Information** - Server names, IDs, and ping timestamps

#### Key Features

- **Startup Initialization**: Scans all loaded assemblies via reflection once at startup
- **Metadata Discovery**: Extracts entity metadata from DbContext using Entity Framework model
- **Service Discovery**: Finds services by scanning for attributes (`ServiceLogic`, `ServiceAction`, etc.)
- **System Validation**: Validates system entities against predefined schema
- **UI Generation Support**: Provides metadata for automatic frontend UI generation

#### Usage Patterns

```csharp
// Access cached metadata
var tableMetadata = Cache.Instance.GetTableMetadata("Widget");
var fieldInfo = tableMetadata.MetadataOutput.fields;

// Check service logic availability
bool hasPostOpLogic = tableMetadata.HasPostOpServiceLogic;

// Get service configurations
var serviceLogics = Cache.Instance.ServiceLogics["Widget"];
var actions = Cache.Instance.ServiceActions["ExportWidgets"];
```

### Permission Cache (`core/xams/Xams.Core/PermissionCache.cs`)

An in-memory permission system optimized for fast authorization checks in multi-server environments:

#### Cached Permission Data

- **Users** - Basic user info and creation timestamps
- **Role Permissions** - HashSet of permission names per role
- **User Roles** - Direct role assignments to users
- **Team Roles** - Role assignments to teams  
- **User Teams** - Team membership mappings

#### Performance Features

- **Thread Safety**: Uses `ConcurrentDictionary` for safe concurrent access
- **Incremental Updates**: Supports updating specific users/roles/teams without full refresh
- **Multi-Server Resilience**: Falls back to database queries for unknown users
- **New User Handling**: 3-second retry delay for recently created users in distributed deployments
- **Permission Resolution**: Combines direct user roles and team-based roles

#### Cache Methods

```csharp
// Get user permissions (with fallback to database)
var permissions = await PermissionCache.GetUserPermissions(userId);
var specificPerms = await PermissionCache.GetUserPermissions(userId, ["TABLE_Widget_CREATE_USER"]);

// Incremental cache updates
await PermissionCache.CacheUserRoles(db, userId);           // Update single user's roles
await PermissionCache.CacheRolePermissions(db, roleId);     // Update single role's permissions
await PermissionCache.CacheTeamRoles(db, teamId);          // Update single team's roles
await PermissionCache.CacheUserTeams(db, userId);          // Update single user's teams

// Full cache refresh (startup)
await PermissionCache.CacheUsers(db);                      // All users
await PermissionCache.CacheRolePermissions(db);            // All role permissions
await PermissionCache.CacheUserRoles(db);                  // All user roles
await PermissionCache.CacheTeamRoles(db);                  // All team roles
await PermissionCache.CacheUserTeams(db);                  // All user teams
```

#### Cache Invalidation

```csharp
// Remove from cache when entities are deleted
PermissionCache.RemoveUser(userId);
PermissionCache.RemoveRole(roleId);
PermissionCache.RemoveTeam(teamId);

// Update permission names when changed
PermissionCache.UpdatePermission(oldName, newName);
PermissionCache.RemovePermission(permissionName);
```

### Cache Initialization

Both caches are initialized during application startup:

```csharp
// Main cache initialization (in StartupService)
await Cache.Initialize(dataService);

// Permission cache initialization (in PermissionCacheJob)
await PermissionCache.CacheUsers(db);
await PermissionCache.CacheRolePermissions(db);
await PermissionCache.CacheUserRoles(db);
await PermissionCache.CacheTeamRoles(db);
await PermissionCache.CacheUserTeams(db);
```

### Performance Benefits

1. **Sub-millisecond Permission Checks**: Permissions resolved from memory instead of database queries
2. **Fast UI Generation**: Entity metadata cached for immediate frontend rendering
3. **Reduced Database Load**: Service discovery and metadata resolved once at startup
4. **Scalable Multi-Server**: Permission cache handles distributed deployments with load balancers
5. **Efficient Updates**: Incremental cache updates minimize refresh overhead

### Cache Monitoring

```csharp
// Check cache state
var lastUpdate = PermissionCache.LastUpdateTime;
var userCount = PermissionCache.Users.Count;
var serverName = Cache.Instance.ServerName;

// Validate cache consistency
var hasUser = PermissionCache.Users.ContainsKey(userId);
```

The cache system is fundamental to Xams' performance, enabling rapid metadata-driven development while maintaining security and consistency across distributed environments.

---

← [Back to Main Documentation](../CLAUDE.md) | [Components Reference](.claude/components.md) →