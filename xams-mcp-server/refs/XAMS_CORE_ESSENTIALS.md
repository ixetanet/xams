# Xams.Core API - Essentials

**Quick Reference** | Version 1.0.10 | [Full API →](XAMS_CORE_API.md)

Essential classes and interfaces for 90% of service logic development.

---

## Service Interfaces

### IServiceLogic

Execute on create, read, update, or delete.

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Create | DataOperation.Update, LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var db = context.GetDbContext<DataContext>();
        var widget = context.GetEntity<Widget>();

        if (context.DataOperation is DataOperation.Create)
        {
            widget.Price = CalculatePrice();
        }

        return ServiceResult.Success();
    }
}
```

### IServiceAction

Execute custom serverside logic.

```csharp
[ServiceAction(nameof(MyAction))]
public class MyAction : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var parameters = context.GetParameters<MyActionParameters>();
        var db = context.GetDbContext<DataContext>();

        // Do something

        return ServiceResult.Success(new { result = "value" });
    }
}
```

### IServiceJob

Execute scheduled jobs.

```csharp
[ServiceJob("MyJobName", "Primary-Queue", "00:05:00")]
public class MyJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<DataContext>();
        // Do something
        return ServiceResult.Success();
    }
}
```

### IServiceStartup

Execute logic on application startup.

```csharp
[ServiceStartup(StartupOperation.Post)]
public class MyStartup : IServiceStartup
{
    public async Task<Response<object?>> Execute(StartupContext context)
    {
        var db = context.GetDbContext<DataContext>();
        // Initialize data
        return ServiceResult.Success();
    }
}
```

[Full Interfaces →](XAMS_CORE_API.md#interfaces)

---

## Service Contexts

### ServiceContext

Used in IServiceLogic - provides access to entity, database, and state.

```csharp
// Get database
var db = context.GetDbContext<DataContext>();

// Get entity being saved
var entity = context.GetEntity<MyEntity>();

// Get entity before update (null on create)
var preEntity = context.GetPreEntity<MyEntity>();

// Check if field changed
if (context.ValueChanged(nameof(MyEntity.Name))) { }

// Get operation type
if (context.DataOperation is DataOperation.Create) { }

// Get stage
if (context.LogicStage is LogicStage.PreOperation) { }

// Get parameters passed from client
var parameters = context.GetParameters<MyParameters>();

// Modify other entities (triggers their service logic)
await context.Create<OtherEntity>(entity);
await context.Update<OtherEntity>(entity);
await context.Delete<OtherEntity>(entity);

// Execute job
await context.ExecuteJob(new JobOptions { JobName = "MyJob" });

// Send SignalR message
await context.HubSend<MyHub>(message);

// Check permissions
var permissions = await context.Permissions(userId, ["Permission1", "Permission2"]);
```

**Key Properties**: ExecutingUserId, DataOperation, LogicStage, Entity, PreEntity, ReadInput

**Key Methods**: GetEntity<T>(), GetPreEntity<T>(), GetDbContext<T>(), ValueChanged(), Create(), Update(), Delete()

### ActionServiceContext

Used in IServiceAction.

```csharp
var db = context.GetDbContext<DataContext>();
var parameters = context.GetParameters<MyActionParameters>();

// Return JSON
return ServiceResult.Success(new { data = "value" });

// Return file
return ServiceResult.Success(new FileData { Stream = fileStream });

// Access uploaded file
var file = context.File;
var stream = file.OpenReadStream();
```

### JobServiceContext

Used in IServiceJob - same as ActionServiceContext.

### StartupContext

Used in IServiceStartup.

```csharp
var db = context.GetDbContext<DataContext>();

// Initialize roles and permissions
await context.SecurityBuilder
    .Role("Admin")
        .Permission("TABLE_Widget_CREATE_SYSTEM")
        .Permission("TABLE_Widget_UPDATE_SYSTEM")
    .Execute();
```

[Full Context API →](XAMS_CORE_API.md#contexts)

---

## Attributes

### Service Logic

```csharp
[ServiceLogic(tableName, dataOperation, logicStage, order = 0)]

// Parameters:
// - tableName: "Widget" or "*" for all entities
// - dataOperation: Create | Read | Update | Delete (flags)
// - logicStage: PreValidation | PreOperation | PostOperation (flags)
// - order: Execution order (optional)
```

### Service Action

```csharp
[ServiceAction("ActionName")]
```

### Service Job

```csharp
[ServiceJob(name, queue, timeSpan, schedule = Interval, daysOfWeek = All)]

// Examples:
[ServiceJob("MyJob", "Queue1", "00:05:00")] // Every 5 minutes
[ServiceJob("MyJob", "Queue1", "03:00:00", JobSchedule.TimeOfDay, DaysOfWeek.Monday)]
```

### Service Startup

```csharp
[ServiceStartup(StartupOperation.Post, order = 0)]
// Post = after system records created, Pre = before
```

### Job Server

```csharp
[JobServer(ExecuteJobOn.One)] // Run on one server only
[JobServer(ExecuteJobOn.One, "ServerName")] // Run on specific server
```

### UI Attributes

```csharp
[UIName]           // Lookup display field
[UIDescription]    // Lookup description field
[UIRequired]       // Required field (UI + backend validation)
[UIReadOnly]       // Read-only (backend enforced)
[UIHide]           // Hidden from UI
[UIRecommended]    // Recommended (UI indicator only)
[UIOption("OptionGroup")] // Dropdown from Options table
[UIDateFormat("lll")] // Date format (Day.js format)
[UIDisplayName("Custom Label")] // Field label override
[CascadeDelete]    // Cascade delete on foreign key
```

[Full Attributes →](XAMS_CORE_API.md#attributes)

---

## Response Handling

### ServiceResult

Static helper for returning responses.

```csharp
// Success
return ServiceResult.Success();
return ServiceResult.Success(data);
return ServiceResult.Success(new { prop = "value" });

// Error (don't throw exceptions!)
return ServiceResult.Error("User-friendly message");
return ServiceResult.Error("Friendly", "Log message");

// File download
return ServiceResult.Success(new FileData {
    Stream = fileStream,
    FileName = "file.txt"
});
```

### Response<T>

```csharp
public interface Response<T>
{
    bool Succeeded { get; }
    T Data { get; }
    string FriendlyMessage { get; }
    string LogMessage { get; }
}

// Usage
var response = await context.Create<Widget>(widget);
if (!response.Succeeded)
{
    return ServiceResult.Error(response.FriendlyMessage);
}
var createdWidget = response.Data;
```

---

## Enums

### DataOperation

```csharp
DataOperation.Create
DataOperation.Read
DataOperation.Update
DataOperation.Delete
DataOperation.Action

// Use with flags:
DataOperation.Create | DataOperation.Update
```

### LogicStage

```csharp
LogicStage.PreValidation  // Before validation attributes checked
LogicStage.PreOperation   // After validation, before save
LogicStage.PostOperation  // After save
```

### PermissionLevel

```csharp
PermissionLevel.System  // All records
PermissionLevel.Team    // User's team records
PermissionLevel.User    // User's records only
```

---

## Base Entities

### BaseEntity

Inherit for standard fields.

```csharp
public class Widget : BaseEntity
{
    public Guid WidgetId { get; set; }
    public string Name { get; set; }

    // BaseEntity provides:
    // - Guid? OwningUserId      // Nullable - ID of owning user
    // - Guid? OwningTeamId      // Nullable - ID of owning team (or null)
    // - DateTime CreatedDate
    // - Guid CreatedById
    // - DateTime UpdatedDate
    // - Guid UpdatedById
    // - bool IsActive
}
```

**Important**: When creating entities manually in service logic, set `OwningTeamId = null` (NOT `Guid.Empty`) if there's no team ownership. Using `Guid.Empty` causes foreign key constraint errors since no Team exists with that ID.

### Entity Rules

1. Must have string Name field or [UIName] attribute
2. No composite primary keys
3. Self-referencing: Use ID only, no navigation property
4. **System entities use base names**: Classes extending `User`, `Team`, `Role`, or `Setting` use the base class name as the table name (e.g., `public class AppUser : User` → table `"User"` in queries/permissions)

```csharp
// ❌ BAD: Self-reference with navigation
public class Comment : BaseEntity
{
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; } // Don't do this
}

// ✅ GOOD: ID only
public class Comment : BaseEntity
{
    public Guid? ParentCommentId { get; set; }
}
```

---

## Dependency Injection

All service classes support constructor injection:

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    private readonly IEmailService _emailService;

    public WidgetService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        await _emailService.SendNotification("Widget created");
        return ServiceResult.Success();
    }
}
```

---

## Common Patterns

### Modify Related Records

```csharp
// Triggers service logic for OtherEntity
var otherEntity = new OtherEntity { ... };
await context.Create<OtherEntity>(otherEntity);
await context.Update<OtherEntity>(existingEntity);
await context.Delete<OtherEntity>(entityToDelete);
```

### Check Field Changes

```csharp
// Always use ValueChanged (returns true on create)
if (context.ValueChanged(nameof(MyEntity.Name)))
{
    // Name was modified
}

// ❌ Don't compare directly:
// if (entity.Name != preEntity.Name) // Wrong!
```

### Access Read Query

```csharp
if (context.DataOperation is DataOperation.Read)
{
    var readInput = context.ReadInput;
    var filters = readInput.filters;
    var orderBy = readInput.orderBy;
}
```

### Execute Jobs

```csharp
await context.ExecuteJob(new JobOptions
{
    JobName = "MyJob",
    Parameters = new { param = "value" }
});
```

---

## Best Practices

1. **Never throw exceptions** - Return `ServiceResult.Error()` instead
2. **Use ValueChanged()** - Don't compare PreEntity/Entity directly
3. **Isolate logic in private methods** - Keep Execute() clean
4. **Use PreOperation when possible** - Better performance
5. **Check operation/stage defensively** - Guard methods with checks
6. **Use dependency injection** - Constructor-based DI is supported

---

## Need More?

- **All Classes & Methods**: [XAMS_CORE_API.md](XAMS_CORE_API.md)
- **Complete Attribute Reference**: [XAMS_CORE_API.md#attributes](XAMS_CORE_API.md#attributes)
- **Pipeline & Repositories**: [XAMS_CORE_API.md#repositories](XAMS_CORE_API.md#repositories)
- **Utilities & Helpers**: [XAMS_CORE_API.md#utils](XAMS_CORE_API.md#utils)
