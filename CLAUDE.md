# ToDo List App - Xams Project Development Guide

# ⚠️ CRITICAL: READ BEFORE ANY IMPLEMENTATION ⚠️

**MANDATORY READING ORDER:**
1. ✅ Read this entire CLAUDE.md file first
2. ✅ Read [Core Components Reference](.claude/components.md) for API documentation
3. ✅ Read [Architecture Deep Dive](.claude/architecture.md) for system understanding
4. ✅ Read [Development Workflows](.claude/workflows.md) for implementation patterns
5. ✅ Only then begin implementation

**DO NOT:**
- ❌ Write code without reading the component documentation
- ❌ Guess API interfaces - they are fully documented in .claude/components.md
- ❌ Use generic React patterns - Xams has specific conventions
- ❌ Skip the examples - they show the correct implementation

**VERIFICATION CHECKLIST:**
Before writing any code, confirm you know:
- [ ] The correct useFormBuilder interface (see components.md line 210-269)
- [ ] How DataTable props work (see components.md line 159-170)
- [ ] The proper FormContainer usage (see components.md line 172-185)
- [ ] Available callbacks like onPostSave (NOT onSaveSuccess)

---

## 🛑 STOP AND CHECK

Before writing your first line of code:

1. **Can you answer these questions?**
   - What are the exact props for useFormBuilder? (If no, read components.md:210-269)
   - What callbacks does FormContainer support? (If no, read components.md:172-185)
   - What methods are available on formBuilder? (If no, read components.md:252-268)

2. **Have you found an example?**
   - The documentation contains working examples for every component
   - Copy the example first, then modify it

3. **Are you guessing?**
   - If you're unsure about any API, STOP and read the documentation
   - Every component interface is fully documented

---

## Table of Contents

### ⚠️ MANDATORY FIRST READS
1. [**READ FIRST: Component API Documentation**](.claude/components.md)
2. [**READ SECOND: Implementation Patterns**](.claude/workflows.md)
3. [**READ THIRD: Architecture Overview**](.claude/architecture.md)

### Then Continue With:
4. [Project Overview](#project-overview)
5. [Framework Overview](#framework-overview)
6. [Quick Reference](#quick-reference)
7. [Common Mistakes to Avoid](#common-mistakes-to-avoid)

---

## Project Overview

### Current Project: ToDo List Application

**Project Name**: XamsProjectApi  
**Project Status**: Initial Setup Phase  
**Frontend**: xams-project (Next.js with Mantine UI)  
**Backend**: XamsProjectApi (C# .NET 8 with Xams Framework 1.0.9)  
**Database**: SQLite (Local development)  

### Project Structure

```
ToDoListApp/
├── CLAUDE.md                    # This development guide
├── XamsProjectApi/              # Backend C# API
│   ├── XamsProjectApi.sln      # Solution file
│   └── XamsProjectApi/         # Main API project
│       ├── DataContext.cs      # Database context (SQLite)
│       ├── Program.cs          # API configuration
│       ├── Migrations/         # EF Core migrations
│       └── *.csproj           # Project file with Xams.Core 1.0.9
└── xams-project/               # Frontend Next.js app
    ├── package.json           # With @ixeta/xams 1.0.9
    ├── src/pages/index.tsx    # Basic home page
    └── ...                    # Standard Next.js structure
```

### Current Implementation Status

**✅ Completed:**
- Basic Xams framework setup (backend + frontend)
- SQLite database configuration
- Initial migration with all Xams system entities
- CORS configuration for development
- Next.js frontend with Mantine UI dependencies

**🔄 In Progress:**
- Todo list entity design and implementation

**📋 Planned:**
- Todo entity with proper Xams attributes
- CRUD operations for todos
- Frontend todo list interface
- User authentication integration
- Priority and category features

### Key Configuration

**Database**: SQLite at `%LocalApplicationData%/myxamsproject_app.db`  
**Backend Port**: Default (check launchSettings.json)  
**Frontend Port**: 3000  
**Admin Dashboard**: Available at `/xams/admin`  

### Development Commands

**Backend:**
```bash
cd XamsProjectApi/XamsProjectApi
dotnet run
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

**Frontend:**
```bash
cd xams-project  
npm run dev
npm run build
npm run lint
```

---

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
- `XamsProjectApi/XamsProjectApi/Program.cs` - API setup
- `XamsProjectApi/XamsProjectApi/DataContext.cs` - Database config

**Service Logic:**
- `XamsProjectApi/XamsProjectApi/Logic/` - Service logic classes (to be created)
- `XamsProjectApi/XamsProjectApi/Actions/` - Custom actions (to be created)

**Entities:**
- `XamsProjectApi/XamsProjectApi/Entities/` - Entity definitions (to be created)
- `Xams.Core/Entities/` - System entities (from NuGet package)

**React Components:**
- `xams-project/src/` - Frontend application
- `@ixeta/xams` package - Core Xams components and hooks

---

## Common Mistakes to Avoid

### ❌ WRONG: Guessing Component APIs
```tsx
// WRONG - These props/methods don't exist
const formBuilder = useFormBuilder({
  recordId: id,  // ❌ Wrong prop name - should be 'id'
});
formBuilder.reset(); // ❌ Method doesn't exist - use clear() or clearEdits()
<FormContainer onSaveSuccess={...}> // ❌ Wrong callback name - use formBuilder.onPostSave
```

### ✅ CORRECT: Use Documented APIs
```tsx
// CORRECT - From components.md documentation
const formBuilder = useFormBuilder({
  tableName: "Todo",
  id: todoId,  // ✅ Correct prop name
  onPostSave: (operation, id, data) => { // ✅ Correct callback
    console.log("Saved:", operation, id, data);
  }
});
// Use documented methods
formBuilder.clear();  // ✅ Reset form completely
formBuilder.clearEdits();  // ✅ Clear unsaved changes
```

### ❌ WRONG: Using Modal incorrectly
```tsx
// WRONG - Modal is not exported from @ixeta/xams
import { Modal } from "@ixeta/xams";  // ❌ Doesn't exist
```

### ✅ CORRECT: Import from Mantine
```tsx
// CORRECT - Modal comes from Mantine
import { Modal } from "@mantine/core";  // ✅ Correct import
```

---

## 📋 Pre-Implementation Checklist

Before implementing ANY feature:

### Documentation Review
- [ ] Have you read the relevant section in components.md?
- [ ] Have you found a working example in the documentation?
- [ ] Have you verified the exact prop names and types?

### For Todo List Implementation Specifically:
- [ ] Review useFormBuilder interface (components.md:210-269)
- [ ] Review DataTable props (components.md:159-170)
- [ ] Review FormContainer usage (components.md:172-185)
- [ ] Check the correct callback names (onPostSave not onSaveSuccess)
- [ ] Verify available methods (clear() not reset())

---

## 🔗 Quick Reference Links

**ALWAYS CONSULT THESE BEFORE CODING:**

- **Frontend Component APIs**: [components.md#frontend-components](.claude/components.md#frontend-components)
  - [useFormBuilder Complete Interface](.claude/components.md) (lines 210-269)
  - [DataTable Component](.claude/components.md) (lines 159-170)
  - [FormContainer Pattern](.claude/components.md) (lines 172-185)

- **Backend Patterns**: [components.md#backend-components](.claude/components.md#backend-components)
  - [Entity Attributes](.claude/components.md) (lines 23-45)
  - [Option System](.claude/components.md) (lines 46-136)

---

## Summary

Xams provides a complete full-stack framework with:

1. **Consistent architecture** through pipeline pattern
2. **Attribute-driven development** for rapid UI creation  
3. **Built-in security** with granular permissions
4. **Extensible design** for custom requirements
5. **Developer productivity** through conventions

**⚠️ REMINDER: For detailed information on specific topics, ALWAYS refer to the linked documentation files in the `.claude/` directory BEFORE writing any code.**

---

## Documentation Location

**Primary Documentation**: All framework documentation is maintained in the `xams-docs-v1/` folder. Always use this location for creating, updating, or referencing documentation rather than creating standalone documentation files.