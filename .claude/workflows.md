# Development Workflows & Best Practices

← [Back to Main Documentation](../CLAUDE.md)

## Table of Contents

1. [Development Workflows](#development-workflows)
2. [Conventions & Best Practices](#conventions--best-practices)

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
    private readonly IEmailService _emailService;

    // Constructor-based dependency injection for custom services
    public ExportWidgets(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var parameters = context.GetParameters<ExportParams>();
        var db = context.GetDbContext<DataContext>();

        var widgets = await db.Widgets
            .Where(w => w.Price > parameters.MinPrice)
            .ToListAsync();

        // Use injected services
        await _emailService.SendNotification($"Export completed: {widgets.Count} widgets");

        // Use ActionServiceContext for framework services
        context.Logger.LogInformation($"Exported {widgets.Count} widgets");

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

### Project-Specific Notes

#### Current Version: 1.0.9

- Latest stable release
- Compatible with .NET 8 and React 18
- Entity Framework Core 8

#### Key Files to Know

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

#### Environment Variables

```bash
# Server identification
SERVER_NAME=Alpha

# Database connection (if not in code)
CONNECTION_STRING="Data Source=app.db"

# CORS settings (development)
ASPNETCORE_ENVIRONMENT=Development
```

#### Build Scripts

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

← [Back to Main Documentation](../CLAUDE.md) | [Testing & Reference](reference.md) →