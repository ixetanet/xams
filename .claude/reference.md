# Testing, Debugging & Extension Points

← [Back to Main Documentation](../CLAUDE.md)

## Table of Contents

1. [Testing & Debugging Guide](#testing--debugging-guide)
2. [Extension Points](#extension-points)
3. [Complete Quick Reference](#complete-quick-reference)

---

## Testing & Debugging Guide

### Development Authentication

During development, bypass authentication using query parameter:

```
http://localhost:3000?userid=f8a43b04-4752-4fda-a89f-62bebcd8240c
```

System user ID: `f8a43b04-4752-4fda-a89f-62bebcd8240c`

### React Setup for Development

```tsx
// _app.tsx or App.tsx
const userId = getQueryParam("userid", router.asPath);

<AuthContextProvider
  apiUrl="https://localhost:8000"
  headers={{
    UserId: userId as string,  // Development only!
  }}
>
```

### Common Error Patterns

#### Permission Denied

```json
{
  "succeeded": false,
  "friendlyMessage": "User does not have permission TABLE_Widget_CREATE_SYSTEM"
}
```

**Solution**: Check role permissions in Admin Dashboard

#### Foreign Key Constraint

```json
{
  "succeeded": false,
  "friendlyMessage": "The DELETE statement conflicted with the REFERENCE constraint"
}
```

**Solution**: Add `[CascadeDelete]` or make FK nullable

#### Required Field Missing

```json
{
  "succeeded": false,
  "friendlyMessage": "The field Name is required"
}
```

**Solution**: Provide required field or remove `[UIRequired]`

### Performance Debugging

#### Enable SQL Logging

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlite(connectionString)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging();
}
```

#### Track Execution ID

Every API call has unique `ExecutionId`:

```csharp
public async Task<Response<object?>> Execute(ServiceContext context)
{
    var executionId = context.ExecutionId;
    Logger.LogInformation($"Execution {executionId}: Processing widget");
}
```

#### Monitor Pipeline Performance

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Read, LogicStage.PostOperation)]
public class WidgetMetricsService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        // Logic here
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 100)
        {
            Logger.LogWarning($"Slow query: {stopwatch.ElapsedMilliseconds}ms");
        }

        return ServiceResult.Success();
    }
}
```

### Transaction Debugging

Check transaction boundaries:

```csharp
public async Task<Response<object?>> Execute(ServiceContext context)
{
    var db = context.GetDbContext<DataContext>();

    // Check if in transaction
    var currentTransaction = db.Database.CurrentTransaction;
    if (currentTransaction != null)
    {
        Logger.LogInformation($"In transaction: {currentTransaction.TransactionId}");
    }

    return ServiceResult.Success();
}
```

---

## Extension Points

### Custom Pipeline Stages

1. **Create Pipeline Stage**:

```csharp
public class CustomValidationStage : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        // Custom validation logic
        if (!IsValid(context.Entity))
        {
            return ServiceResult.Error("Validation failed");
        }

        // Continue pipeline
        return await base.Execute(context);
    }
}
```

2. **Register in Pipeline**:

```csharp
// In PipelineBuilder configuration
builder.Add(new CustomValidationStage());
```

### Custom Attributes

1. **Create Attribute**:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class UITooltipAttribute : Attribute
{
    public string Tooltip { get; }

    public UITooltipAttribute(string tooltip)
    {
        Tooltip = tooltip;
    }
}
```

2. **Process in Metadata**:

```csharp
// In MetadataRepository
var tooltipAttr = property.GetCustomAttribute<UITooltipAttribute>();
if (tooltipAttr != null)
{
    fieldInfo.Tooltip = tooltipAttr.Tooltip;
}
```

### Custom Service Discovery

Override service discovery:

```csharp
public class CustomStartupService : StartupService
{
    protected override void RegisterServices(IServiceCollection services)
    {
        // Custom registration logic
        services.Scan(scan => scan
            .FromAssemblyOf<CustomMarker>()
            .AddClasses(classes => classes.AssignableTo<IServiceLogic>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}
```

### Admin Dashboard Customization

The admin dashboard is embedded in `wwwroot/admin/`:

1. Build custom dashboard in `core/xams-workspace/admin-dash/`
2. Run build script to update embedded files
3. Dashboard available at `/xams/admin`

### Custom Bulk Operations

```csharp
[BulkService(nameof(Widget))]
public class WidgetBulkService : IBulkService
{
    public async Task<Response<object?>> Execute(BulkServiceContext context)
    {
        var widgets = context.GetEntities<Widget>();

        // Bulk processing logic
        foreach (var widget in widgets)
        {
            widget.ProcessedDate = DateTime.UtcNow;
        }

        return ServiceResult.Success();
    }
}
```

---

## Complete Quick Reference

### Attribute Cheat Sheet

| Attribute                  | Purpose              | Example                                                    |
| -------------------------- | -------------------- | ---------------------------------------------------------- |
| `[UIName]`                 | Lookup display field | `[UIName] public string Name { get; set; }`                |
| `[UIDescription]`          | Lookup description   | `[UIDescription] public string Info { get; set; }`         |
| `[UIDisplayName("Label")]` | Field label          | `[UIDisplayName("Unit Price")]`                            |
| `[UIRequired]`             | Required field       | `[UIRequired] public string Code { get; set; }`            |
| `[UIRecommended]`          | Recommended field    | `[UIRecommended] public string Email { get; set; }`        |
| `[UIHide]`                 | Hide from UI         | `[UIHide] public string Internal { get; set; }`            |
| `[UIReadOnly]`             | Read-only in UI      | `[UIReadOnly] public DateTime Created { get; set; }`       |
| `[UIOption("Name")]`       | Option group link    | `[UIOption("Status")] public Guid? StatusId { get; set; }` |
| `[UIDateFormat("lll")]`    | Date format          | `[UIDateFormat("MM/DD/YYYY")]`                             |
| `[UICharacterLimit(50)]`   | Max length           | `[UICharacterLimit(100)]`                                  |
| `[UINumberRange(0,100)]`   | Number range         | `[UINumberRange(1, 10)]`                                   |
| `[UIOrder(1)]`             | Field order          | `[UIOrder(10)]`                                            |
| `[CascadeDelete]`          | Delete cascade       | `[CascadeDelete] public Guid? ChildId { get; set; }`       |

### API Endpoints

| Endpoint            | Method | Purpose           |
| ------------------- | ------ | ----------------- |
| `/xams/create`      | POST   | Create records    |
| `/xams/read`        | POST   | Query records     |
| `/xams/update`      | PATCH  | Update records    |
| `/xams/delete`      | DELETE | Delete records    |
| `/xams/upsert`      | POST   | Upsert records    |
| `/xams/bulk`        | POST   | Bulk operations   |
| `/xams/action`      | POST   | Custom actions    |
| `/xams/file`        | POST   | File upload       |
| `/xams/metadata`    | POST   | Entity metadata   |
| `/xams/permissions` | POST   | Check permissions |
| `/xams/whoami`      | GET    | Current user      |

### useAuthRequest Hook

```tsx
// useAuthRequest - API operations
const authRequest = useAuthRequest();
await authRequest.create(tableName, fields, parameters?);
await authRequest.read(readRequest);
await authRequest.update(tableName, fields, parameters?);
await authRequest.delete(tableName, id, parameters?);
await authRequest.upsert(tableName, fields, parameters?);
await authRequest.bulkCreate(entities, parameters?);
await authRequest.bulkUpdate(entities, parameters?);
await authRequest.bulkDelete(entities, parameters?);
await authRequest.bulkUpsert(entities, parameters?);
await authRequest.bulk(bulkRequest);
await authRequest.action(actionName, parameters?, fileName?);
await authRequest.file(formData);
await authRequest.metadata(tableName);
await authRequest.tables(tag?);
await authRequest.whoAmI();
await authRequest.hasAnyPermissions(permissions);
await authRequest.hasAllPermissions(permissions);
await authRequest.execute(requestParams); // Low-level request method
```

### ServiceContext Methods

```csharp
// Entity access
T entity = context.GetEntity<T>();
T preEntity = context.GetPreEntity<T>();

// Database access
DbContext db = context.GetDbContext<TContext>();

// CRUD operations (triggers pipeline)
await context.Create(entity);
await context.Update(entity);
await context.Delete(entity);

// Field checks
bool changed = context.ValueChanged("FieldName");

// Permissions
string[] perms = await context.Permissions(userId, ["PERM1", "PERM2"]);

// Jobs (not Actions)
Guid jobHistoryId = await context.ExecuteJob(new JobOptions {
    JobName = "JobName",
    Parameters = parameters
});

// Context properties
Guid userId = context.ExecutingUserId;
DataOperation operation = context.DataOperation;
LogicStage stage = context.LogicStage;
Guid executionId = context.ExecutionId;
```

### Common ServiceResult Patterns

```csharp
// Success responses
return ServiceResult.Success();
return ServiceResult.Success(data);
return ServiceResult.Success(new { field = value });

// Error responses
return ServiceResult.Error("Error message");
return ServiceResult.Error("User message", "Log message");

// File responses
return ServiceResult.Success(new FileData
{
    Stream = fileStream,
    FileName = "export.xlsx",
    ContentType = "application/octet-stream"
});
```

### Migration Commands

```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Drop database
dotnet ef database drop
```

### Package Commands

```bash
# Backend (NuGet)
dotnet pack                          # Create package
dotnet nuget push Xams.Core.1.0.9.nupkg -s source

# Frontend (npm)
npm run build:rollup                 # Build library
npm publish --access public          # Publish to npm
```

### Development URLs

- Admin Dashboard: `http://localhost:PORT/xams/admin?userid=GUID`
- API Base: `http://localhost:PORT/xams/`
- System User ID: `f8a43b04-4752-4fda-a89f-62bebcd8240c`

---

← [Back to Main Documentation](../CLAUDE.md) | [Architecture](architecture.md) →