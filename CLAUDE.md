# Xams Framework - Claude Code Development Guide

## Table of Contents

1. [Framework Overview](#framework-overview)
2. [Architecture Deep Dive](#architecture-deep-dive)
3. [Core Components Reference](#core-components-reference)
4. [Development Workflows](#development-workflows)
5. [Conventions & Best Practices](#conventions--best-practices)
6. [Testing & Debugging Guide](#testing--debugging-guide)
7. [Extension Points](#extension-points)
8. [Quick Reference](#quick-reference)

---

## Framework Overview

### Purpose & Philosophy

Xams is a full-stack framework designed for rapid application development with a focus on:

- **Convention over configuration**: Attributes drive UI behavior
- **Pipeline-based architecture**: Consistent CRUD operations with hooks
- **Security-first design**: Built-in permission system with ownership model
- **Developer productivity**: Minimal boilerplate, maximum functionality

### Technology Stack

- **Backend**: C# .NET 8, Entity Framework Core 8
- **Frontend**: React 18, Mantine UI 7, TypeScript
- **Databases**: SQLite, PostgreSQL, MySQL, SQL Server
- **Package Management**: NuGet (backend), npm (frontend)
- **Current Version**: 1.0.9

### Key Differentiators

1. **Attribute-Driven UI**: Entity attributes automatically configure frontend behavior
2. **Pipeline Architecture**: Every CRUD operation flows through a consistent pipeline
3. **Integrated Security**: Role-based permissions with record ownership
4. **Automatic API Generation**: Entities automatically get full CRUD endpoints
5. **Admin Dashboard**: Built-in admin interface at `/xams/admin`

### Repository Structure

```
xams/
├── core/
│   ├── xams/                     # Backend C# projects
│   │   ├── Xams.Core/           # Core framework library
│   │   ├── MyXProject.Web/      # Web API project template
│   │   ├── MyXProject.Data/     # Data layer with migrations
│   │   ├── MyXProject.Services/ # Service logic implementation
│   │   └── MyXProject.Common/   # Shared entities
│   └── xams-workspace/
│       ├── ixeta-xams/          # React component library
│       ├── examples-app/        # Example implementations
│       └── admin-dash/          # Admin dashboard
├── xams-docs-v1/                # Documentation site
└── cli/                         # Xams CLI tool
```

---

## Architecture Deep Dive

### Pipeline Architecture

The pipeline implements the Chain of Responsibility pattern for all CRUD operations:

```
Request → PreValidation Logic → Permission Check → PreOperation Logic → Save → PostOperation Logic → Response
```

#### Pipeline Stages

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

#### Pipeline Context Flow

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

### Service Layer Architecture

#### Service Types

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

### Security Architecture

#### Permission Hierarchy

```
User → UserRole → Role → RolePermission → Permission
User → TeamUser → Team → TeamRole → Role
```

#### Record Ownership Model

- **BaseEntity** provides `OwningUserId` and `OwningTeamId`
- At least one ownership field must be set
- Permissions check ownership based on level:
  - **User Level**: Can only access own records
  - **Team Level**: Can access team records
  - **System Level**: Can access all records

#### Permission Format

```
TABLE_{TableName}_{Operation}_{Level}
Example: TABLE_Widget_CREATE_SYSTEM
```

### Transaction Management

- Every API call wrapped in single transaction
- Rollback on any exception or `ServiceResult.Error()`
- `ExecutionId` tracks related operations
- Bulk operations maintain transaction consistency

---

## Core Components Reference

### Backend Components

#### DataService (`core/xams/Xams.Core/Services/DataService.cs`)

Central service handling all CRUD operations:

- Manages pipeline execution
- Handles transaction boundaries
- Provides repository access
- Coordinates service logic execution

#### Entity Attributes (`core/xams/Xams.Core/Attributes/`)

**UI Configuration Attributes:**

- `[UIName]` - Field shown in lookups
- `[UIDescription]` - Secondary lookup text
- `[UIDisplayName("Label")]` - Form field label
- `[UIRequired]` - Makes field required
- `[UIRecommended]` - Shows blue indicator
- `[UIHide]` - Hides from UI (server-only)
- `[UIReadOnly]` - Prevents UI updates
- `[UIOption("OptionSet")]` - Links to option set
- `[UIDateFormat("lll")]` - Date display format
- `[UICharacterLimit(100)]` - Max character length
- `[UINumberRange(0, 100)]` - Number constraints
- `[UIOrder(1)]` - Field display order

**Behavior Attributes:**

- `[CascadeDelete]` - Delete behavior configuration
- `[UISetFieldFromLookup("LookupIdProperty")]` - Auto-populate fields from lookup
- `[UIProxy]` - Proxy field for related data

#### Pipeline Stages (`core/xams/Xams.Core/Pipeline/Stages/`)

Key pipeline stages:

- `PipePermissions` - Security validation
- `PipePreValidation` - Early validation
- `PipeEntityCreate/Update/Delete` - Core operations
- `PipeExecuteServiceLogic` - Logic execution
- `PipeSetDefaultFields` - Default value setting

#### Repository Pattern (`core/xams/Xams.Core/Repositories/`)

- `DataRepository` - CRUD operations
- `MetadataRepository` - Entity metadata
- `SecurityRepository` - Permission checks

### Frontend Components

#### Core React Components (`core/xams-workspace/ixeta-xams/src/components/`)

**DataTable Component:**

```tsx
<DataTable
  tableName="Widget"
  filters={[{ field: "Price", operator: ">", value: 10 }]}
  orderBy={[{ field: "Name", direction: "asc" }]}
  maxResults={50}
  searchable={true}
  onCreate={(record) => console.log(record)}
/>
```

**FormBuilder System:**

```tsx
const formBuilder = useFormBuilder({
  tableName: "Widget",
  defaults: { Price: 9.99 },
});

<FormContainer formBuilder={formBuilder}>
  <Field name="Name" />
  <Field name="Price" />
  <SaveButton />
</FormContainer>;
```

**DataGrid Component:**

```tsx
<DataGrid
  tableName="OrderLines"
  parentId={orderId}
  editable={true}
  onSave={(records) => console.log(records)}
/>
```

#### React Hooks (`core/xams-workspace/ixeta-xams/src/hooks/`)

**useAuthRequest:**

```tsx
const authRequest = useAuthRequest();
await authRequest.create('Widget', { Name: 'Test' });
await authRequest.read('Widget', { filters: [...] });
await authRequest.update('Widget', widgetId, { Price: 19.99 });
await authRequest.delete('Widget', widgetId);
await authRequest.action('MyAction', { param: 'value' });
```

**useFormBuilder:**

```tsx
const formBuilder = useFormBuilder({
  tableName: "Widget",
  recordId: widgetId,
  onSave: (record) => console.log("Saved:", record),
});
```

#### Context Providers (`core/xams-workspace/ixeta-xams/src/contexts/`)

**AuthContext:**

- Manages API authentication
- Provides `useAuthRequest` hook
- Handles API URL configuration

**AppContext:**

- Global application state
- UI preferences
- Cached metadata

---

## Development Workflows

### Creating a New Entity

1. **Create Entity Class** (`MyXProject.Common/Entities/Widget.cs`):

```csharp
[Table("Widget")]
public class Widget : BaseEntity  // Inherit for ownership
{
    public Guid WidgetId { get; set; }  // Primary key convention

    [UIRequired]
    [UIDisplayName("Widget Name")]
    public string Name { get; set; }

    [UIRecommended]
    public decimal Price { get; set; }

    [UIOption("WidgetType")]
    public Guid? WidgetTypeId { get; set; }
    public Option? WidgetType { get; set; }

    // Foreign key example
    public Guid CompanyId { get; set; }  // Required (non-nullable)
    public Company Company { get; set; }

    [CascadeDelete]
    public Guid? AddressId { get; set; }  // Optional with cascade
    public Address? Address { get; set; }
}
```

2. **Add to DbContext** (`MyXProject.Data/DataContext.cs`):

```csharp
public DbSet<Widget> Widgets { get; set; }
```

3. **Create Migration**:

```bash
dotnet ef migrations add AddWidget
dotnet ef database update
```

4. **Add Service Logic** (`MyXProject.Services/Logic/WidgetService.cs`):

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    private readonly IEmailService _emailService;

    // Constructor-based dependency injection for custom services
    public WidgetService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var widget = context.GetEntity<Widget>();
        var db = context.GetDbContext<DataContext>();

        // Business logic here
        widget.Price = await CalculatePrice(db, widget);

        // Use injected services
        await _emailService.SendNotification("Widget created");
        
        // Use ServiceContext for logger
        context.Logger.LogInformation("Widget processing completed");

        return ServiceResult.Success();
    }
}
```

5. **Create React UI**:

```tsx
// List view
<DataTable tableName="Widget" />;

// Form view
const formBuilder = useFormBuilder({ tableName: "Widget" });
<FormContainer formBuilder={formBuilder}>
  <Field name="Name" />
  <Field name="Price" />
  <Field name="WidgetTypeId" />
  <SaveButton />
</FormContainer>;
```

### Creating a Custom Action

1. **Create Action Class** (`MyXProject.Services/Actions/ExportWidgets.cs`):

```csharp
[ServiceAction(nameof(ExportWidgets))]
public class ExportWidgets : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var parameters = context.GetParameters<ExportParams>();
        var db = context.GetDbContext<DataContext>();

        var widgets = await db.Widgets
            .Where(w => w.Price > parameters.MinPrice)
            .ToListAsync();

        // Return JSON
        return ServiceResult.Success(new {
            count = widgets.Count,
            widgets = widgets
        });

        // Or return file
        var stream = GenerateExcel(widgets);
        return ServiceResult.Success(new FileData
        {
            Stream = stream,
            FileName = "widgets.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });
    }

    private class ExportParams
    {
        public decimal MinPrice { get; set; }
    }
}
```

2. **Call from React**:

```tsx
const authRequest = useAuthRequest();

// JSON response
const response = await authRequest.action("ExportWidgets", { MinPrice: 10 });
if (response.succeeded) {
  console.log(response.data);
}

// File download
await authRequest.action("ExportWidgets", { MinPrice: 10 }, "widgets.xlsx");
```

### Creating a Scheduled Job

1. **Create Job Class** (`MyXProject.Services/Jobs/WidgetPriceUpdateJob.cs`):

```csharp
[JobServer(ExecuteJobOn.One)]  // Run on single server
[JobTimeZone("Eastern Standard Time")]  // Consistent timezone
[ServiceJob("Update Widget Prices", "Price-Queue", "03:00:00",
    JobSchedule.TimeOfDay, DaysOfWeek.Monday | DaysOfWeek.Friday)]
public class WidgetPriceUpdateJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<DataContext>();
        var serviceContext = context.GetServiceContext();

        var widgets = await db.Widgets.ToListAsync();
        foreach (var widget in widgets)
        {
            widget.Price *= 1.05m;  // 5% increase
            await serviceContext.Update<Widget>(widget);
        }

        return ServiceResult.Success();
    }
}
```

### Implementing Complex Service Logic

```csharp
[ServiceLogic(nameof(Order), DataOperation.Create, LogicStage.PostOperation)]
public class OrderService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var order = context.GetEntity<Order>();
        var db = context.GetDbContext<DataContext>();

        // Create audit record
        await context.Create(new OrderAudit
        {
            OrderId = order.OrderId,  // PK available in PostOperation
            Action = "Created",
            Timestamp = DateTime.UtcNow
        });

        // Update inventory
        foreach (var line in order.OrderLines)
        {
            var product = await db.Products.FindAsync(line.ProductId);
            product.Stock -= line.Quantity;
            await context.Update(product);  // Triggers product service logic
        }

        // Send notification (job)
        await context.ExecuteJob(new JobOptions
        {
            JobName = "SendOrderNotification",
            Parameters = new 
            {
                OrderId = order.OrderId,
                CustomerEmail = order.Customer.Email
            }
        });

        return ServiceResult.Success();
    }
}
```

---

## Conventions & Best Practices

### Naming Conventions

#### Entity Conventions

- **Primary Key**: `{EntityName}Id` (e.g., `WidgetId`)
- **Foreign Key**: `{RelatedEntity}Id` (e.g., `CompanyId`)
- **Navigation Property**: Same as related entity (e.g., `Company`)
- **Table Name**: Use `[Table("Widget")]` attribute

#### Service Class Conventions

- **Service Logic**: `{Entity}Service` (e.g., `WidgetService`)
- **Actions**: Descriptive verb-noun (e.g., `ExportWidgets`)
- **Jobs**: `{Purpose}Job` (e.g., `WidgetCleanupJob`)

#### Permission Naming

```
TABLE_{TableName}_{Operation}_{Level}
ACTION_{ActionName}
JOB_{JobName}
CUSTOM_{CustomPermission}
```

### Code Patterns

#### Always Use ServiceContext for CRUD

```csharp
// CORRECT - Triggers service logic pipeline
await context.Create(entity);
await context.Update(entity);
await context.Delete(entity);

// WRONG - Bypasses service logic
db.Widgets.Add(entity);
await db.SaveChangesAsync();
```

#### Use Constructor-Based Dependency Injection

```csharp
// CORRECT - Constructor injection for custom services, ServiceContext for framework services
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
        // Use injected custom services
        await _emailService.SendNotification("Widget created");
        
        // Use ServiceContext for framework services
        context.Logger.LogInformation("Widget processed");
        var db = context.GetDbContext<DataContext>();
        
        return ServiceResult.Success();
    }
}

// WRONG - Service locator pattern not supported
public class WidgetService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var emailService = context.GetService<IEmailService>(); // Does not exist
        return ServiceResult.Success();
    }
}
```

#### Prefer PreOperation for Performance

```csharp
// GOOD - Single save operation
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation)]

// AVOID unless needed - Forces immediate save
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PostOperation)]
```

#### Check Value Changes in Updates

```csharp
if (context.DataOperation == DataOperation.Update)
{
    if (context.ValueChanged(nameof(Widget.Price)))
    {
        // Price was modified
        var oldPrice = context.GetPreEntity<Widget>().Price;
        var newPrice = context.GetEntity<Widget>().Price;
    }
}
```

#### Handle Transactions Properly

```csharp
public async Task<Response<object?>> Execute(ServiceContext context)
{
    try
    {
        // All operations within transaction
        await context.Create(entity1);
        await context.Update(entity2);
        await context.Delete(entity3);

        return ServiceResult.Success();
    }
    catch (Exception ex)
    {
        // Transaction automatically rolled back
        return ServiceResult.Error($"Operation failed: {ex.Message}");
    }
}
```

### Common Pitfalls

#### 1. Direct DbContext Modifications

```csharp
// WRONG - Bypasses pipeline
db.Widgets.Update(widget);
await db.SaveChangesAsync();

// CORRECT
await context.Update(widget);
```

#### 2. Composite Primary Keys

```csharp
// NOT SUPPORTED
public class OrderLine
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    // Must have single PK instead
}

// CORRECT
public class OrderLine
{
    public Guid OrderLineId { get; set; }  // Single PK
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
}
```

#### 3. UIHide Misunderstanding

```csharp
[UIHide]  // Hidden from UI and CANNOT be filtered/queried
public string SecretField { get; set; }

[UIHide(true)]  // Hidden from UI but CAN be filtered/queried
public string QueryableHiddenField { get; set; }
```

#### 4. Missing Base Entity Inheritance

```csharp
// WRONG - No ownership support
public class Widget
{
    public Guid WidgetId { get; set; }
}

// CORRECT - Ownership enabled
public class Widget : BaseEntity
{
    public Guid WidgetId { get; set; }
}
```

#### 5. Incorrect Service Logic Order

```csharp
// Services execute in order value
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation, 100)]
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation, 200)]
// 100 executes before 200
```

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

## Quick Reference

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
| `[UIOption("Set")]`        | Option set link      | `[UIOption("Status")] public Guid? StatusId { get; set; }` |
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

### React Hooks

```tsx
// useAuthRequest - API operations
const authRequest = useAuthRequest();
await authRequest.create(table, data);
await authRequest.read(table, { filters, orderBy, fields });
await authRequest.update(table, id, data);
await authRequest.delete(table, id);
await authRequest.bulk(operations);
await authRequest.action(name, params, fileName);
await authRequest.file(formData);
await authRequest.hasAnyPermissions([...]);
await authRequest.hasAllPermissions([...]);

// useFormBuilder - Form management
const formBuilder = useFormBuilder({
  tableName: 'Widget',
  recordId: id,
  defaults: { field: value },
  onSave: (record) => {},
  onChange: (field, value) => {}
});
formBuilder.setField(name, value);
formBuilder.getField(name);
formBuilder.save();
formBuilder.reset();

// useAdminPermission - Admin checks
const { isAdmin, loading } = useAdminPermission();

// useColor - Theme detection
const { colorScheme } = useColor();
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

## Project-Specific Notes

### Current Version: 1.0.9

- Latest stable release
- Compatible with .NET 8 and React 18
- Entity Framework Core 8

### Key Files to Know

**Configuration:**

- `core/xams/MyXProject.Web/Program.cs` - API setup
- `core/xams/MyXProject.Data/DataContext.cs` - Database config
- `core/xams-workspace/examples-app/src/pages/_app.tsx` - React setup

**Service Logic:**

- `core/xams/MyXProject.Services/Logic/` - Service logic classes
- `core/xams/MyXProject.Services/Actions/` - Custom actions
- `core/xams/MyXProject.Services/Jobs/` - Scheduled jobs

**Entities:**

- `core/xams/MyXProject.Common/Entities/` - Entity definitions
- `core/xams/Xams.Core/Entities/` - System entities

**React Components:**

- `core/xams-workspace/ixeta-xams/src/components/` - Core components
- `core/xams-workspace/ixeta-xams/src/hooks/` - Custom hooks
- `core/xams-workspace/ixeta-xams/src/contexts/` - Context providers

### Environment Variables

```bash
# Server identification
SERVER_NAME=Alpha

# Database connection (if not in code)
CONNECTION_STRING="Data Source=app.db"

# CORS settings (development)
ASPNETCORE_ENVIRONMENT=Development
```

### Build Scripts

```bash
# Backend build
cd core/xams
dotnet build

# Frontend build
cd core/xams-workspace/ixeta-xams
npm run build:rollup

# Admin dashboard
cd core/xams-workspace/admin-dash
npm run build
```

---

## Summary

Xams provides a complete full-stack framework with:

1. **Consistent architecture** through pipeline pattern
2. **Attribute-driven development** for rapid UI creation
3. **Built-in security** with granular permissions
4. **Extensible design** for custom requirements
5. **Developer productivity** through conventions

When developing with Xams:

- Follow entity naming conventions
- Use attributes to configure UI behavior
- Implement service logic at appropriate pipeline stages
- Always use ServiceContext for CRUD operations
- Leverage the admin dashboard for configuration

For new features:

1. Start with entity design
2. Add appropriate attributes
3. Create migrations
4. Implement service logic if needed
5. Build UI with provided components

Remember: The framework handles the complexity, you focus on business logic.

---

## Documentation Location

**Primary Documentation**: All framework documentation is maintained in the `xams-docs-v1/` folder. Always use this location for creating, updating, or referencing documentation rather than creating standalone documentation files.
