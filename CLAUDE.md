# Xams Framework - Claude Code Development Guide

## Table of Contents

1. [Framework Overview](#framework-overview)
2. [Architecture Deep Dive](.claude/architecture.md)
3. [Core Components Reference](.claude/components.md)
4. [Development Workflows](.claude/workflows.md)
5. [Quick Reference](#quick-reference)

**Detailed Documentation:**
- [Architecture & Cache System](.claude/architecture.md)
- [Backend & Frontend Components](.claude/components.md)
- [Development Workflows & Best Practices](.claude/workflows.md)
- [Testing, Debugging & Extension Points](.claude/reference.md)

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
│   │   ├── MyXProject.Web/      # Web API project (includes all layers)
│   │   │   ├── Actions/         # Custom actions
│   │   │   ├── Logic/           # Service logic implementation
│   │   │   ├── Entities/        # Entity definitions
│   │   │   ├── Migrations/      # Database migrations
│   │   │   └── ...              # Other project files
│   │   └── Xams.Console/        # Console application
│   └── xams-workspace/
│       ├── ixeta-xams/          # React component library
│       ├── examples-app/        # Example implementations
│       └── admin-dash/          # Admin dashboard
├── xams-docs-v1/                # Documentation site
└── cli/                         # Xams CLI tool
```

### Essential Entity Pattern

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
}
```

### Essential Service Logic Pattern

```csharp
[ServiceLogic(nameof(Widget), DataOperation.Create, LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var widget = context.GetEntity<Widget>();
        var db = context.GetDbContext<DataContext>();

        // Business logic here
        return ServiceResult.Success();
    }
}
```

### Essential React Pattern

```tsx
// DataTable for listing
<DataTable tableName="Widget" />

// FormBuilder for editing
const formBuilder = useFormBuilder({ tableName: "Widget" });
<FormContainer formBuilder={formBuilder}>
  <Field name="Name" />
  <Field name="Price" />
  <SaveButton />
</FormContainer>
```

---

## Quick Reference

### Core Attributes

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[UIName]` | Lookup display field | `[UIName] public string Name { get; set; }` |
| `[UIDisplayName("Label")]` | Field label | `[UIDisplayName("Widget Name")]` |
| `[UIRequired]` | Required field | `[UIRequired] public string Code { get; set; }` |
| `[UIRecommended]` | Recommended field | `[UIRecommended] public string Email { get; set; }` |
| `[UIOption("Name")]` | Option group link | `[UIOption("Status")] public Guid? StatusId { get; set; }` |
| `[UIHide]` | Hide from UI | `[UIHide] public string Internal { get; set; }` |
| `[CascadeDelete]` | Delete cascade | `[CascadeDelete] public Guid? ChildId { get; set; }` |

### API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/xams/create` | POST | Create records |
| `/xams/read` | POST | Query records |
| `/xams/update` | PATCH | Update records |
| `/xams/delete` | DELETE | Delete records |
| `/xams/action` | POST | Custom actions |
| `/xams/metadata` | POST | Entity metadata |

### useAuthRequest Hook

```tsx
const authRequest = useAuthRequest();
await authRequest.create('Widget', { Name: 'Test' });
await authRequest.read({ tableName: 'Widget', filters: [...] });
await authRequest.update('Widget', { WidgetId: widgetId, Price: 19.99 });
await authRequest.delete('Widget', widgetId);
await authRequest.action('MyAction', { param: 'value' });
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

// Context properties
Guid userId = context.ExecutingUserId;
DataOperation operation = context.DataOperation;
LogicStage stage = context.LogicStage;
```

### Development Setup

**System User ID**: `f8a43b04-4752-4fda-a89f-62bebcd8240c`

**Development URL**: `http://localhost:3000?userid=f8a43b04-4752-4fda-a89f-62bebcd8240c`

**Admin Dashboard**: `http://localhost:PORT/xams/admin?userid=GUID`

### Migration Commands

```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Key Files

**Configuration:**
- `core/xams/MyXProject.Web/Program.cs` - API setup
- `core/xams/MyXProject.Web/DataContext.cs` - Database config

**Service Logic:**
- `core/xams/MyXProject.Web/Logic/` - Service logic classes
- `core/xams/MyXProject.Web/Actions/` - Custom actions

**Entities:**
- `core/xams/MyXProject.Web/Entities/` - Entity definitions
- `core/xams/Xams.Core/Entities/` - System entities

**React Components:**
- `core/xams-workspace/ixeta-xams/src/components/` - Core components
- `core/xams-workspace/ixeta-xams/src/hooks/` - Custom hooks

---

## Summary

Xams provides a complete full-stack framework with:

1. **Consistent architecture** through pipeline pattern
2. **Attribute-driven development** for rapid UI creation  
3. **Built-in security** with granular permissions
4. **Extensible design** for custom requirements
5. **Developer productivity** through conventions

**For detailed information on specific topics, refer to the linked documentation files in the `.claude/` directory.**

---

## Documentation Location

**Primary Documentation**: All framework documentation is maintained in the `xams-docs-v1/` folder. Always use this location for creating, updating, or referencing documentation rather than creating standalone documentation files.