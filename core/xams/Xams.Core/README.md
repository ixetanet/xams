# Xams.Core

Xams.Core is the .NET side of the Xams framework. You describe your data as Entity Framework entities, and Xams provides:
- a secured CRUD API
- metadata the React package (`@ixeta/xams`) uses to build forms and tables
- a pipeline for business rules
- actions, jobs and an admin dashboard

This guide shows how to use it.

- [Install](#install)
- [Set up the host](#set-up-the-host)
- [Define entities](#define-entities)
- [Add business rules](#add-business-rules)
- [Add actions](#add-actions)
- [Read data in server code](#read-data-in-server-code)
- [Write data in server code](#write-data-in-server-code)
- [Save an edited aggregate](#save-an-edited-aggregate)
- [Jobs, startup code and bulk hooks](#jobs-startup-code-and-bulk-hooks)
- [Permissions](#permissions)
- [HTTP API](#http-api)

## Install

```bash
dotnet add package Xams.Core
```

Xams.Core targets .NET 8 and Entity Framework Core 8.

## Set up the host

### 1. Create the DbContext

Inherit from `XamsDbContext` and expose a `DbSet` for every table Xams should serve.

```csharp
using Microsoft.EntityFrameworkCore;
using Xams.Core.Base;

public class DataContext : XamsDbContext
{
    public DbSet<Widget> Widgets { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        base.OnConfiguring(options);
        options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
```

- **Configure the provider in `OnConfiguring`.** Xams creates the context through its parameterless constructor, so options passed in through dependency injection aren't used.
- **Always call the base methods.**
- **Extending the built-in User, Team, Role or Setting.** Inherit from it (for example `AppUser : User`) and use the generic base, `XamsDbContext<AppUser>`. The table is still called `User`.

### 2. Register Xams

```csharp
using Xams.Core;

builder.Services.AddXamsServices<DataContext>();   // or AddXamsServices<DataContext, AppUser>()

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.AddXamsApi(options =>
{
    options.UseDashboard = true;          // admin dashboard under /xams
    options.RequireAuthorization = true;  // apply ASP.NET authorization to the endpoints
    options.GetUserId = GetXamsUserId;    // HttpContext → the caller's Xams UserId
});

app.Run();
```

`GetUserId` is a `Func<HttpContext, Task<Guid>>` that turns the authenticated caller into the ID of a `User` record. Without it, Xams reads a `UserId` request header. That's handy for local development, but it identifies the caller without authenticating them.

`UrlPath` changes the API prefix from `xams`. The React package assumes `/xams`.

### 3. Create the schema

```bash
dotnet ef migrations add Initial
dotnet ef database update
```

On startup, Xams discovers your tables, seeds the system user, roles and permissions, and starts scheduled jobs. Restart the app after changing entities or attributes.

## Define entities

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Entities;

[Table(nameof(Widget))]
public class Widget : BaseEntity
{
    public Guid WidgetId { get; set; }

    [UIName, UIRequired, UICharacterLimit(100)]
    public string Name { get; set; } = string.Empty;

    [UINumberRange(0, 10000)]
    public decimal Price { get; set; }

    [UIOption("Widget_Type")]
    public Guid? WidgetTypeId { get; set; }
    public Option? WidgetType { get; set; }
}
```

**Conventions:**
- **Table name.** The API name is the EF table name (`Widget`), not the `DbSet` name (`Widgets`).
- **Primary key.** A `Guid` named `<Table>Id`.
- **Lookups.** A foreign key `XId` paired with a navigation property `X`. The lookup shows the target's `[UIName]` field, or its `Name` property.
- **`BaseEntity`** adds ownership fields (`OwningUserId`, `OwningTeamId`), `CreatedDate`/`CreatedById`, `UpdatedDate`/`UpdatedById` and `IsActive`, and Xams fills them in. Inheriting it is optional; without it, records have no owner.

**Attributes:**

| Attribute | Effect |
| --- | --- |
| `[UIName]`, `[UIDescription]` | Field shown (and the secondary text) when the record appears in a lookup |
| `[UIDisplayName("Label")]`, `[UIOrder(n)]` | Field label and order in the UI |
| `[UIRequired]`, `[UIRecommended]` | Required (enforced on API writes) or recommended field |
| `[UIReadOnly]`, `[UICreateOnly]` | Can't be set through the API, or only on create |
| `[UIHide]` | Left out of metadata and reads. `[UIHide(queryable: true)]` still allows filtering on it |
| `[UICharacterLimit(n)]`, `[UINumberRange(min, max)]` | Validated on API writes |
| `[UIDateFormat("lll")]` | Display format for a date |
| `[UIOption("Group")]` | Limits an `Option` lookup to one option group |
| `[UISetFieldFromLookup(nameof(XId))]` | Copies the lookup's display value into this string field |
| `[OwningUser]` | Treats another user-ID field as an owner for permission checks |
| `[CascadeDelete]` | On a foreign key: deleting the parent deletes this record instead of clearing the key |
| `[UIMultiSelect(ownerField, targetField)]` | Exposes a junction collection as a multi-select field |

To set a class-level override on an inherited field, pass the field name as the attribute's extra argument, for example `[UIRequired(nameof(OwningUserId))]` on the class.

**Multi-select.** Put a collection of junction entities on the owner and name the junction's two keys:

```csharp
public class EmailMessage : BaseEntity
{
    public Guid EmailMessageId { get; set; }

    [UIMultiSelect(nameof(EmailMessageContact.EmailMessageId), nameof(EmailMessageContact.ContactId))]
    public ICollection<EmailMessageContact> To { get; set; } = null!;
}
```

The junction and target tables need their own `DbSet`s. On an API create or update, sending `To` replaces the links: an empty array removes them all, and leaving `To` out changes nothing.

## Add business rules

A class with `[ServiceLogic]` runs inside the pipeline whenever a matching write (or read) happens: through the API, through actions, or through `context.Create/Update/Delete` in server code.

```csharp
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

[ServiceLogic(nameof(Widget), DataOperation.Create | DataOperation.Update, LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var widget = context.GetEntity<Widget>();

        if (context.DataOperation == DataOperation.Update && context.ValueChanged(nameof(Widget.Price)))
        {
            var before = context.GetPreEntity<Widget>();
            // compare before.Price with widget.Price
        }

        if (widget.Price < 0)
        {
            return ServiceResult.Error("Price can't be negative.");
        }

        widget.Name = widget.Name.Trim();
        return ServiceResult.Success();
    }
}
```

**Stages:**

| Stage | Use it to |
| --- | --- |
| `PreValidation` | Fill in or normalize values before required-field and UI validation run |
| `PreOperation` | Apply business rules and make final changes to the entity, after validation |
| `PostOperation` | React after the record is saved. Still inside the transaction, so returning an error rolls it back |

**Rules of thumb:**
- Operations and stages are flags, so one class can handle several; check `context.DataOperation` and `context.LogicStage` before acting.
- Change the entity returned by `GetEntity<T>()` instead of calling `context.Update` on the same record; the pipeline saves your changes.
- `GetPreEntity<T>()` is the stored record, available on update and delete.
- Use `"*"` as the table name to run for every table, and `order` to sequence several classes on the same table.
- Returning `ServiceResult.Error(...)` stops the operation and returns the message to the caller.
- Constructor dependency injection works.

## Add actions

An action is a named command for anything that isn't plain CRUD.

```csharp
using Microsoft.EntityFrameworkCore;

[ServiceAction(nameof(RenameWidget))]
public class RenameWidget : IServiceAction
{
    public class Input
    {
        public Guid WidgetId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var input = context.GetParameters<Input>();
        var db = context.GetDbContext<DataContext>();
        var widget = await db.Widgets.SingleOrDefaultAsync(x => x.WidgetId == input.WidgetId);
        if (widget == null)
        {
            return ServiceResult.Error("Widget not found.");
        }

        widget.Name = input.Name;
        var updated = await context.Update<Widget>(widget);   // runs the pipeline as the caller
        return ServiceResult.Success(new { updated.WidgetId });
    }
}
```

- **Calling it:** `POST /xams/action` with `{ "name": "RenameWidget", "parameters": { ... } }`. From React, use `authRequest.action("RenameWidget", { ... })`.
- **Permission:** callers need `ACTION_RenameWidget`.
- **Transaction:** the action runs in one transaction. Returning an error or throwing rolls back everything it wrote.
- **Files:** to return a file, return `ServiceResult.Success(fileData)` with a `FileData`.

## Read data in server code

Build reads with `ServiceQuery`, the server-side twin of the React `Query` builder:

```csharp
var query = new ServiceQuery("WidgetId", "Name", "Price")
    .From(nameof(Widget))
    .LeftJoin("Widget.WidgetTypeId", "Option.OptionId", "type", ["Label"])
    .Where("Price", ">", 10)
    .And(ServiceQuery.Exp("Name", "Contains", "Pro").Or("Name", "Contains", "Max"))
    .OrderBy("Name")
    .Top(100);

var output = await context.Read(query);
foreach (IDictionary<string, object?> row in output.results)
{
    var name = row["Name"];
    var typeLabel = row["type.Label"];
}
```

**What else the query builder offers:**
- Operators are `==`, `!=`, `>`, `>=`, `<`, `<=` and `Contains`.
- `Join` is an inner join; `LeftJoin` keeps rows that have no match.
- `Denormalize()` nests joined rows under their alias instead of returning one flat row per combination.
- `Top` sets the page size, which defaults to 50, and `Page` picks the page.

**Read modes:**

| Call | Security | Read hooks |
| --- | --- | --- |
| `context.Read(query)` | The caller's permissions, including joined tables | Skipped |
| `context.Read(query, appendUIInfo: true)` | The caller's permissions | Run, and each row gets `_ui_info_` |
| `context.Read(query, bypassSecurity: true)` | Full read access to the queried tables | Skipped |
| `context.GetDbContext<DataContext>()` + LINQ | None | Skipped |

## Write data in server code

| Call | What it does |
| --- | --- |
| `await context.Create<Widget>(widget)` | Runs the create pipeline (hooks and permissions) as the caller |
| `await context.Update<Widget>(widget)` | Runs the update pipeline. Replaces the whole record, so load it first and change it |
| `await context.Delete(widget)` | Runs the delete pipeline, including cascades |
| `context.GetDbContext<DataContext>()` + `SaveChangesAsync` | Writes directly: no hooks, no permission checks |

These calls throw on failure. Inside an action, that rolls back the action. Pass a user ID as the first argument to run as someone else.

## Save an edited aggregate

Some screens load a large set of related data, let the user add, change and delete rows at any level, and save only when they click **Save**. For those, pair a Get action and a Save action that share the same `ServiceQuery` definitions:
- The Get action reads the data and returns it.
- The Save action hands the edited JSON to an `AggregateUpdater`.
- The updater works out what to create, update and delete, in the root table and in the joined tables, and writes only what changed in one `SaveChanges`.

### Share the queries

```csharp
public static class TaskQueries
{
    public static ServiceQuery Task(Guid taskId) =>
        new ServiceQuery("*").From("Task").Where("TaskId", "==", taskId);

    public static ServiceQuery Travel(Guid taskId) =>
        new ServiceQuery("*")
            .Top(10000)                                   // must cover every row the query selects
            .From("TaskTravel")
            .LeftJoin("TaskTravel.TaskTravelId", "TaskTravelMonth.TaskTravelId", "month", ["*"])
            .Where("TaskId", "==", taskId)
            .Denormalize();                               // required when a join is saved
}
```

### The Get action

```csharp
[ServiceAction(nameof(GetTask))]
public class GetTask : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var taskId = context.GetParameters<Input>().TaskId;
        var loadedAt = DateTime.UtcNow;
        var task = await context.Read(TaskQueries.Task(taskId), bypassSecurity: true);
        var travel = await context.Read(TaskQueries.Travel(taskId), bypassSecurity: true);
        return ServiceResult.Success(new { loadedAt, task = task.results[0], travel = travel.results });
    }

    public class Input { public Guid TaskId { get; set; } }
}
```

Read with `bypassSecurity: true`, because the save reads that way too. Both actions must see the same rows. Otherwise rows the user couldn't see would be missing from what they send back, and would be deleted.

### The Save action

```csharp
using System.Text.Json;

[ServiceAction(nameof(SaveTask))]
public class SaveTask : IServiceAction
{
    public class Input
    {
        public Guid TaskId { get; set; }
        public DateTime LoadedAt { get; set; }
        public JsonElement? Task { get; set; }
        public JsonElement? Travel { get; set; }
    }

    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var input = context.GetParameters<Input>();
        // Check here that the caller may edit this task: the save itself checks no permissions

        var updater = new AggregateUpdater(context)
            .Record(TaskQueries.Task(input.TaskId), input.Task)
            .Collection(TaskQueries.Travel(input.TaskId), input.Travel)
            .LoadedAt(input.LoadedAt);

        var changes = await updater.DiffAsync();
        // Business rules go here (see below)
        await updater.SaveAsync();

        return ServiceResult.Success(changes.Summary());
    }
}
```

The client sends back exactly what the Get action returned, after editing it:

```jsonc
"travel": [
  { "TaskTravelId": "a1…", "Destination": "Dayton",           // changed fields are updated
    "month": [
      { "TaskTravelMonthId": "m1…", "Trips": 2 },             // existing: updated if changed
      { "Trips": 1 }                                          // no ID: created under a1…
    ] }                                                       // m2… left out: deleted
]
```

### Sections

- `Collection(query, json)` takes a JSON array of root records.
- `Record(query, json)` takes a single JSON object; its root record is never deleted. If the record has no ID and the query filters on its key with `==`, it's created with that key.
- `query.ToUpdater(context, json)` is shorthand for an updater with one `Collection`.

### What gets saved

The query decides what the save may touch.

| | Root | Child join (from the parent's key) | Reference join (to the joined table's key) |
| --- | --- | --- | --- |
| Saved by default | Yes | Yes; turn off with `o => o.ReadOnly("alias")` | No; turn on with `o => o.Writable("alias")` |
| New record | Created; the query's top-level `==` filters are set on it | Created; its link to the parent comes from where it's nested | Created; the parent's foreign key must point at it |
| Record left out | Deleted with its children (`Collection` only) | Deleted, with its children | Never deleted |

Pass the options as the section's last argument, for example `.Collection(query, json, o => o.Writable("category").ReadOnlyFields("Code"))`.

Many-to-many works too: root → junction is a child join, and junction → target is a reference join.

**Select the keys.** The query must select the primary key of the root and of every join the save writes. `*` does this; with an explicit field list, include the key.
- **Why:** a record that comes back without its key looks new, so the stored record it came from would look left out, and the save would delete it and insert a copy.
- **Enforced:** the updater refuses such a query with a `Setup` error before reading anything.
- **Exception:** joins the save doesn't write don't need their key.

**Fields:**
- Only fields the query selected (`*` means every field without `[UIHide]`) and the client sent are compared, and only changed ones are written.
- The updater fills in `CreatedDate`, `CreatedById`, `UpdatedDate`, `UpdatedById`, `OwningUserId` and `IsActive` the way the API does.
- Keys, computed columns and fields listed in `ReadOnlyFields` are never written.
- Values are compared after the same conversion the API applies, so Guid casing, extra decimal places, `null` vs `""` and sub-millisecond date differences don't count as changes.

**Missing vs. empty:**
- A missing or `null` section or join array means "don't touch".
- An empty array means "delete every row of that kind the query selects".
- Rows the query doesn't select, because of its filters or joins, are never changed.

### Conflicts

- **Stale `UpdatedDate`.** If the client sends a record's `UpdatedDate` and the save changes that record, a different value in the database is a conflict: someone else changed it after it was loaded.
- **`LoadedAt(time)`.** Deleting a row that was created or changed after that time is a conflict instead of a delete.
- **`IgnoreConflicts()`** turns both checks off.

### Business rules

The save skips the pipeline, so rules in your `[ServiceLogic]` classes don't run for its writes. Put them in the Save action instead:
- **Validate** the input before building the updater.
- **Inspect and adjust** the result of `DiffAsync()`:
  - `changes.Creates`, `changes.Updates` and `changes.Deletes` list everything the save will write.
  - Change a `changes.Created<T>()` instance to change what's inserted.
  - Call `update.Set("Field", value)` to add or override a field change.
- **Write more** through `context.GetDbContext<T>()`. Those writes go out in the same `SaveChanges`.
- **Throw** to roll back the whole action.

### Errors

Problems are reported before anything is written, as an `AggregateUpdateException` whose `Errors` list each problem's kind, table, record ID and field:

| Kind | Examples |
| --- | --- |
| `Setup` | A query that doesn't select the key of the root or of a join it saves; a saved join without `Denormalize()`; a saved join with no matching EF foreign key; `Distinct`; a `Top` smaller than the rows the query selects |
| `OutOfScope` | An ID that exists but isn't in the query's rows; changing a field the query filters on |
| `Invalid` | The same record twice; a value that can't be converted; a reference that disagrees with its foreign key |
| `Conflict` | See [Conflicts](#conflicts); also the same shared record edited differently in two places |
| `Database` | The database rejected the save |

Catch the exception to return the errors to the client, or let it propagate; the action then fails and rolls back.

### What the save doesn't do

- Run `[ServiceLogic]` or bulk hooks.
- Apply UI validation such as `[UIRequired]` or `[UICharacterLimit]`.
- Check permissions. The action's `ACTION_<Name>` permission is the only check, so verify in the action that the caller may edit the data.
- Run Xams cascade deletes. Rows outside the query that point at a deleted row are handled by your database's foreign-key rules.
- Fill `[UISetFieldFromLookup]` fields.

**Audit** follows each table's audit settings. To skip it for one save, call `context.GetDbContext<DataContext>().SetAuditEnabled(false)` first.

## Jobs, startup code and bulk hooks

**Scheduled jobs:**

```csharp
[ServiceJob(nameof(ClearExpiredCodes), "maintenance", "00:05:00")]   // every 5 minutes
public class ClearExpiredCodes : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<DataContext>();
        // ...
        return ServiceResult.Success();
    }
}
```

- **Scheduling.** Use `JobSchedule.TimeOfDay` with a UTC time such as `"18:00:00"` to run daily. `DaysOfWeek` limits which days.
- **Not a transaction.** A job isn't wrapped in one; manage your own if you need all-or-nothing.
- **Manual runs.** Callers with `JOB_<Name>` can trigger a job by hand.

**Startup code:**

```csharp
[ServiceStartup(StartupOperation.Post)]   // Pre runs before system records are seeded, Post after
public class SeedDefaults : IServiceStartup
{
    public async Task<Response<object?>> Execute(StartupContext context)
    {
        // Make this idempotent: it runs on every start
        return ServiceResult.Success();
    }
}
```

**Bulk hooks.** A class with `[BulkService(BulkStage.Pre)]` or `[BulkService(BulkStage.Post)]` that implements `IBulkService` runs once per request, not once per record:
- `Pre` runs before the records of an API write are processed.
- `Post` runs after them, and after an action or job that wrote through the pipeline.

Use bulk hooks for checks that span several records.

## Permissions

Xams seeds a permission for every table, action and job. Grant permissions to roles, and roles to users or teams, in the admin dashboard.

| Permission | Allows |
| --- | --- |
| `TABLE_<Table>_<CREATE/READ/UPDATE/DELETE/ASSIGN>_<USER/TEAM/SYSTEM>` | The operation on records the user owns (`USER`), their teams own (`TEAM`), or all records (`SYSTEM`). `ASSIGN` allows changing a record's owner |
| `TABLE_<Table>_IMPORT`, `TABLE_<Table>_EXPORT` | Importing or exporting the table |
| `ACTION_<Name>` | Calling an action |
| `JOB_<Name>` | Triggering a job by hand |
| `HUB_<Name>` | Connecting to a SignalR hub |

Permissions apply to API calls, to `context.Read(query)`, and to `context.Create/Update/Delete`. They don't apply to direct `DbContext` access or to `AggregateUpdater` saves.

## HTTP API

The React package (`@ixeta/xams`) wraps these endpoints; you rarely call them directly.

| Method and path | Body |
| --- | --- |
| `POST /xams/read` | `{ tableName, fields, filters, joins, orderBy, maxResults, page }` |
| `POST /xams/create` | `{ tableName, fields }`, or `{ entities: [...] }` for several |
| `PATCH /xams/update` | `{ tableName, fields }`. Only the fields sent are changed |
| `DELETE /xams/delete` | `{ tableName, id, fields: {} }` |
| `POST /xams/bulk` | `{ creates, updates, deletes }` in one transaction |
| `POST /xams/action` | `{ name, parameters }` |
| `POST /xams/file` | Multipart action call with a file |
| `POST /xams/metadata` | `{ method: "table_metadata", parameters: { tableName } }` |
| `POST /xams/permissions` | `{ method: "has_permissions", parameters: { permissionNames } }` |
| `GET /xams/whoami` | The caller's user record |

Responses use one envelope: `{ succeeded, friendlyMessage, logMessage, data }`. Field names in `fields` keep their C# casing, and filter values are sent as strings.
