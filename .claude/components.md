# Xams Core Components Reference

← [Back to Main Documentation](../CLAUDE.md)

## Table of Contents

1. [Backend Components](#backend-components)
2. [Frontend Components](#frontend-components)

---

## Backend Components

### DataService (`core/xams/Xams.Core/Services/DataService.cs`)

Central service handling all CRUD operations:

- Manages pipeline execution
- Handles transaction boundaries
- Provides repository access
- Coordinates service logic execution

### Entity Attributes (`core/xams/Xams.Core/Attributes/`)

**UI Configuration Attributes:**

- `[UIName]` - Field shown in lookups
- `[UIDescription]` - Secondary lookup text
- `[UIDisplayName("Label")]` - Form field label
- `[UIRequired]` - Makes field required
- `[UIRecommended]` - Shows blue indicator
- `[UIHide]` - Hides from UI (server-only)
- `[UIReadOnly]` - Prevents UI updates
- `[UIOption("Name")]` - Links to option group by Name field
- `[UIDateFormat("lll")]` - Date display format
- `[UICharacterLimit(100)]` - Max character length
- `[UINumberRange(0, 100)]` - Number constraints
- `[UIOrder(1)]` - Field display order

**Behavior Attributes:**

- `[CascadeDelete]` - Delete behavior configuration
- `[UISetFieldFromLookup("LookupIdProperty")]` - Auto-populate fields from lookup
- `[UIProxy]` - Proxy field for related data

### Option System (`core/xams/Xams.Core/Entities/Option.cs`)

The Option system provides standardized dropdown/select functionality with admin-managed values.

**Option Entity Structure:**

```csharp
[Table(nameof(Option))]
public class Option
{
    public Guid OptionId { get; set; }          // Primary key
    
    [UIName]                                    // Display field in lookups
    [MaxLength(250)]
    public string? Label { get; set; }          // User-facing display text
    
    [UIDisplayName("Option Name")]              // Grouping identifier
    [MaxLength(250)]
    public string? Name { get; set; }           // Groups related options
    
    [MaxLength(250)]
    public string? Value { get; set; }          // Optional additional data
    
    public int? Order { get; set; }             // Display sequence
    
    [UIHide]                                    // System use only
    [MaxLength(250)]
    public string? Tag { get; set; }            // System labeling (e.g., "System")
}
```

**Usage Pattern:**

```csharp
public class Widget : BaseEntity
{
    public Guid WidgetId { get; set; }
    
    [UIOption("Priority")]                      // Links to options where Name="Priority"
    public Guid? PriorityId { get; set; }       // Foreign key to Option
    public Option? Priority { get; set; }       // Navigation property
    
    [UIOption("Status")]                        // Links to options where Name="Status"
    public Guid? StatusId { get; set; }
    public Option? Status { get; set; }
}
```

**Admin Dashboard Management:**

- **Name**: Group identifier (e.g., "Priority", "Status", "Category")
- **Label**: User-visible text (e.g., "High Priority", "In Progress", "Electronics")
- **Value**: Optional additional data for business logic
- **Order**: Controls display sequence in dropdowns
- **Tag**: System labeling (typically "System" for framework-managed options)

**Key Features:**

- **Automatic UI Generation**: Fields with `[UIOption]` render as dropdowns
- **Runtime Management**: Business users can add/modify options without deployments
- **Consistent Querying**: Options automatically filtered by Name field
- **Ordered Display**: Options display according to Order field
- **Foreign Key Safety**: Referential integrity maintained through Option relationships

**React Integration:**

```tsx
// Automatic dropdown rendering
<Field name="PriorityId" />     // Renders dropdown with Priority options
<Field name="StatusId" />       // Renders dropdown with Status options

// DataTable filtering
<DataTable 
  tableName="Widget"
  filters={[{ field: "StatusId", operator: "=", value: activeStatusId }]}
/>
```

**Common Option Sets:**

- **Priority**: "Low", "Medium", "High", "Critical"
- **Status**: "Draft", "Active", "Inactive", "Archived"
- **Category**: Domain-specific groupings
- **Type**: Classification options

**Performance Notes:**

- Options are queried by `WHERE Name = 'OptionSetName'`
- Consider caching for frequently-used option sets
- Index the Name field for better query performance

### Pipeline Stages (`core/xams/Xams.Core/Pipeline/Stages/`)

Key pipeline stages:

- `PipePermissions` - Security validation
- `PipePreValidation` - Early validation
- `PipeEntityCreate/Update/Delete` - Core operations
- `PipeExecuteServiceLogic` - Logic execution
- `PipeSetDefaultFields` - Default value setting

### Repository Pattern (`core/xams/Xams.Core/Repositories/`)

- `DataRepository` - CRUD operations
- `MetadataRepository` - Entity metadata
- `SecurityRepository` - Permission checks

---

## Frontend Components

### Core React Components (`core/xams-workspace/ixeta-xams/src/components/`)

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

### React Hooks (`core/xams-workspace/ixeta-xams/src/hooks/`)

**useAuthRequest:**

```tsx
const authRequest = useAuthRequest();
await authRequest.create('Widget', { Name: 'Test' });
await authRequest.read({ tableName: 'Widget', filters: [...] });
await authRequest.update('Widget', { WidgetId: widgetId, Price: 19.99 });
await authRequest.delete('Widget', widgetId);
await authRequest.action('MyAction', { param: 'value' });
```

**useFormBuilder:**

```tsx
const formBuilder = useFormBuilder({
  tableName: "Widget",
  id: widgetId,
  onPostSave: (operation, id, data) => console.log("Saved:", operation, id, data),
});
```

**Complete useFormBuilder Interface:**

```tsx
const formBuilder = useFormBuilder({
  tableName: 'Widget',                 // Required: Entity name
  id?: string | null,                  // Optional: Record ID for updates
  metadata?: MetadataResponse,         // Optional: Pre-loaded metadata
  defaults?: FieldValue[],             // Optional: Default field values
  snapshot?: any,                      // Optional: Original record data
  lookupExclusions?: LookupExclusions[], // Optional: Lookup filters
  lookupQueries?: LookupQuery[],       // Optional: Lookup queries
  canUpdate?: boolean,                 // Optional: Override update permission
  canCreate?: boolean,                 // Optional: Override create permission
  onPreValidate?: PreSaveEvent,        // Optional: Before validation hook
  onPreSave?: PreSaveEvent,            // Optional: Before save hook (can cancel)
  onPostSave?: PostSaveEvent,          // Optional: After save hook
  forceShowLoading?: boolean,          // Optional: Force loading display
  keepLoadingOnSuccess?: boolean       // Optional: Keep loading after success
});

// Returned properties
formBuilder.metadata                   // MetadataResponse | undefined
formBuilder.dispatch                   // React dispatch function
formBuilder.data                       // Current form data (typed as T)
formBuilder.snapshot                   // Original data for updates (typed as T)
formBuilder.firstInputRef              // React ref for first input focus
formBuilder.lookupExclusions           // Array of lookup exclusions
formBuilder.lookupQueries              // Array of lookup queries
formBuilder.canUpdate                  // boolean - Update permission
formBuilder.canCreate                  // boolean - Create permission
formBuilder.canRead                    // {canRead: boolean, message: string}
formBuilder.defaults                   // FieldValue[] | undefined
formBuilder.validationMessages         // ValidationMessage[]
formBuilder.isLoading                  // boolean - Loading state
formBuilder.isSubmitted                // boolean - Form submitted state
formBuilder.operation                  // "CREATE" | "UPDATE"
formBuilder.stateType                  // Internal state type
formBuilder.tableName                  // string - Entity name
formBuilder.onPreValidateRef           // React ref for pre-validate event
formBuilder.onPreSaveRef               // React ref for pre-save event
formBuilder.onPostSaveRef              // React ref for post-save event

// Returned methods
formBuilder.setSnapshot(snapshot, forceShowLoading?)  // Set data to edit
formBuilder.reload(reloadDataTables = true)           // Refresh current record
formBuilder.setField(field, value)                    // Set field value
formBuilder.setFieldError(field, message)             // Set field validation error
formBuilder.isDirty(field?)                           // Check if form/field is dirty
formBuilder.addDataTable(dataTable)                   // Register child DataTable
formBuilder.addRequiredField(fieldName)               // Mark field as required
formBuilder.removeRequiredField(fieldName)            // Remove required marking
formBuilder.reloadDataTables()                        // Refresh all child DataTables
formBuilder.save(preValidate?, preSave?, postSave?)   // Save with optional event overrides
formBuilder.saveSilent(parameters?)                   // Save without UI feedback
formBuilder.load(id, forceLoading?)                   // Load specific record by ID
formBuilder.clearEdits()                              // Clear unsaved changes
formBuilder.clear()                                   // Reset form completely
formBuilder.validate()                                // Manual validation (returns boolean)
formBuilder.setShowForceLoading(loading)              // Control forced loading state
```

### Context Providers (`core/xams-workspace/ixeta-xams/src/contexts/`)

**AuthContext:**

- Manages API authentication
- Provides `useAuthRequest` hook
- Handles API URL configuration

**AppContext:**

- Global application state
- UI preferences
- Cached metadata

### Additional Hooks

**useAdminPermission:**

```tsx
const { isAdmin, loading } = useAdminPermission();
```

**useColor:**

```tsx
const { colorScheme } = useColor();
```

---

← [Back to Main Documentation](../CLAUDE.md) | [Development Workflows](workflows.md) →