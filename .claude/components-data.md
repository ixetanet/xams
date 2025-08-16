# Xams Data Display Components Reference

← [Back to Main Documentation](../CLAUDE.md) | [Form Components](components-forms.md)

## Table of Contents

1. [DataTable](#datatable)
2. [DataTableSelectable](#datatableselectable)
3. [DataGrid](#datagrid)
4. [useAuthRequest Hook](#useauthrequest-hook)

---

## DataTable

A comprehensive data table component with CRUD operations, searching, sorting, and inline forms.

### Import
```tsx
import { DataTable } from "@ixeta/xams";
```

### Core Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `tableName` | `string` | ✅ | Entity name to display |
| `title` | `string` | ❌ | Table title |
| `disabledMessage` | `string` | ❌ | Message shown when table is disabled |
| `maxResults` | `number` | ❌ | Maximum records per page (default: 50) |
| `fields` | `DataTableField[]` | ❌ | Fields to display (defaults to all) |
| `additionalFields` | `string[]` | ❌ | Extra fields to query but not display |
| `columnWidths` | `string[]` | ❌ | Column width percentages (e.g., ["30%", "70%"]) |

### Query Props

| Prop | Type | Description |
|------|------|-------------|
| `filters` | `ReadFilter[]` | Filter conditions |
| `orderBy` | `ReadOrderBy[]` | Sort order |
| `joins` | `ReadJoin[]` | Table joins |
| `except` | `ReadExcept[]` | Exclude specific records |

### UI Props

| Prop | Type | Default | Description |
|------|---------|---------|-------------|
| `scrollable` | `boolean` | `false` | Enable horizontal scrolling |
| `searchable` | `boolean` | `true` | Show search box |
| `selectable` | `"single" \| "multiple"` | - | Enable row selection |
| `pagination` | `boolean` | `true` | Show pagination controls |
| `showActiveSwitch` | `boolean` | `false` | Show active/inactive toggle |
| `showOptions` | `boolean` | `true` | Show import/export options |
| `confirmDelete` | `boolean` | `true` | Confirm before delete |
| `refreshInterval` | `number` | - | Auto-refresh interval (ms) |

### Form Props

| Prop | Type | Description |
|------|------|-------------|
| `formTitle` | `string` | Modal form title |
| `formFields` | `string[]` | Fields to show in form |
| `formFieldDefaults` | `FieldValue[]` | Default values for form |
| `formLookupExclusions` | `LookupExclusions[]` | Exclude lookup values |
| `formLookupQueries` | `LookupQuery[]` | Filter lookup queries |
| `formCloseOnCreate` | `boolean` | Close form after create |
| `formCloseOnUpdate` | `boolean` | Close form after update |
| `formCloseOnEscape` | `boolean` | Close form on ESC key |
| `formZIndex` | `number` | Form modal z-index |
| `formMaxWidth` | `number` | Maximum form width |
| `formMinWidth` | `number` | Minimum form width |
| `formHideSaveButton` | `boolean` | Hide save button |

### Event Props

| Prop | Type | Description |
|------|------|-------------|
| `onInitialLoad` | `(results: any[]) => void` | After first data load |
| `onDataLoaded` | `(data: ReadResponse) => void` | After any data load |
| `onRowClick` | `(record: any) => boolean` | Handle row clicks (return true to open form) |
| `onPostDelete` | `(record: any) => void` | After record deletion |
| `onPageChange` | `(page: number) => void` | Page navigation |
| `formOnOpen` | `(operation: "CREATE" \| "UPDATE", record: any) => void` | Form opened |
| `formOnClose` | `() => void` | Form closed |
| `formOnPreSave` | `(data: any, params?: any) => void` | Before form save |
| `formOnPostSave` | `(operation: "CREATE" \| "UPDATE", record: any) => void` | After form save |

### Permission Props

| Prop | Type | Description |
|------|------|-------------|
| `canCreate` | `boolean` | Override create permission |
| `canUpdate` | `boolean` | Override update permission |
| `canDelete` | `boolean` | Override delete permission |
| `canDeactivate` | `boolean` | Show deactivate option |
| `canImport` | `boolean` | Show import option |
| `canExport` | `boolean` | Show export option |

### Custom Rendering Props

| Prop | Type | Description |
|------|------|-------------|
| `customForm` | `(formBuilder, disclosure) => ReactNode` | Custom form component |
| `appendCustomForm` | `(formBuilder) => ReactNode` | Append to default form |
| `formAppendButton` | `(formBuilder) => ReactNode` | Custom form buttons |
| `customCreateButton` | `(openForm) => ReactNode` | Custom create button |
| `customRow` | `(record) => ReactNode` | Custom row rendering |
| `deleteConfirmation` | `(record) => Promise<{title?, message?, showPrompt?}>` | Custom delete confirm |

### Type Definitions

```tsx
interface ReadFilter {
  field: string;
  operator: "=" | "!=" | ">" | "<" | ">=" | "<=" | "contains" | "startswith" | "endswith";
  value: any;
}

interface ReadOrderBy {
  field: string;
  direction: "asc" | "desc";
}

interface DataTableField {
  header: string | ((ref: DataTableRef) => ReactNode);
  body: (record: any, ref: DataTableRef) => ReactNode;
}

interface FieldValue {
  field: string;
  value: any;
}
```

### DataTable Ref Methods

```tsx
interface DataTableRef {
  refresh(): void;                    // Refresh data
  openForm(record?: any): void;       // Open create/edit form
  getRecords(): any[];                // Get current records
  setRecords(fn: (prev) => any[]): void; // Update records
  showLoading(): void;                // Show loading state
  sort(field: string): void;          // Sort by field
  dataTableId: string;                // Unique table ID
  Metadata?: MetadataResponse;        // Entity metadata
}
```

### Usage Examples

#### Basic Table
```tsx
<DataTable tableName="Widget" />
```

#### Filtered and Sorted Table
```tsx
<DataTable 
  tableName="Widget"
  filters={[
    { field: "Price", operator: ">", value: 10 },
    { field: "Status", operator: "=", value: "active" }
  ]}
  orderBy={[
    { field: "CreatedDate", direction: "desc" }
  ]}
  maxResults={25}
/>
```

#### Custom Fields
```tsx
<DataTable 
  tableName="Widget"
  fields={[
    "Name",
    "Price",
    {
      header: "Actions",
      body: (record, ref) => (
        <Button onClick={() => handleAction(record)}>
          Process
        </Button>
      )
    }
  ]}
/>
```

#### With Form Configuration
```tsx
<DataTable 
  tableName="Widget"
  formTitle="Widget Details"
  formFields={["Name", "Price", "CategoryId", "Description"]}
  formFieldDefaults={[
    { field: "Status", value: "draft" }
  ]}
  formCloseOnCreate={true}
  formOnPostSave={(operation, record) => {
    console.log(`Widget ${operation}: ${record.WidgetId}`);
  }}
/>
```

#### Advanced Features
```tsx
const tableRef = useRef<DataTableRef>();

<DataTable 
  ref={tableRef}
  tableName="Widget"
  searchable={true}
  selectable="multiple"
  confirmDelete={true}
  deleteConfirmation={async (record) => ({
    title: "Delete Widget?",
    message: `Are you sure you want to delete ${record.Name}?`,
    showPrompt: true
  })}
  onRowClick={(record) => {
    console.log("Clicked:", record);
    return true; // Open form
  }}
  customCreateButton={(openForm) => (
    <Button variant="gradient" onClick={openForm}>
      Add New Widget
    </Button>
  )}
  refreshInterval={30000} // Refresh every 30 seconds
/>

// Use ref methods
tableRef.current?.refresh();
tableRef.current?.openForm({ Name: "New Widget" });
```

---

## DataTableSelectable

A variant of DataTable optimized for selecting records.

### Import
```tsx
import { DataTableSelectable } from "@ixeta/xams";
```

### Additional Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `onSelectionChange` | `(selected: SelectedRow[]) => void` | ❌ | Selection change handler |
| `selectedRows` | `SelectedRow[]` | ❌ | Controlled selection |

### Usage Example

```tsx
const [selected, setSelected] = useState<SelectedRow[]>([]);

<DataTableSelectable
  tableName="Widget"
  selectable="multiple"
  selectedRows={selected}
  onSelectionChange={setSelected}
  fields={["Name", "Price"]}
/>

// Access selected records
selected.forEach(({ id, row }) => {
  console.log(`Selected: ${row.Name} (${id})`);
});
```

---

## DataGrid

An editable grid component for inline data editing.

### Import
```tsx
import { DataGrid } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `tableName` | `string` | ✅ | Entity name |
| `parentId` | `string` | ❌ | Parent record ID for relationships |
| `parentField` | `string` | ❌ | Parent relationship field |
| `fields` | `string[]` | ❌ | Fields to display |
| `filters` | `ReadFilter[]` | ❌ | Filter conditions |
| `orderBy` | `ReadOrderBy[]` | ❌ | Sort order |
| `canCreate` | `boolean` | ❌ | Allow adding rows |
| `canUpdate` | `boolean` | ❌ | Allow editing |
| `canDelete` | `boolean` | ❌ | Allow deletion |
| `onChange` | `(data: any[]) => void` | ❌ | Data change handler |

### Usage Example

```tsx
<DataGrid
  tableName="WidgetItem"
  parentId={widgetId}
  parentField="WidgetId"
  fields={["Name", "Quantity", "Price"]}
  canCreate={true}
  canUpdate={true}
  canDelete={true}
  onChange={(items) => {
    console.log("Grid data changed:", items);
  }}
/>
```

---

## useAuthRequest Hook

Core hook for making authenticated API requests.

### Import
```tsx
import { useAuthRequest } from "@ixeta/xams";
```

### Methods

```tsx
interface AuthRequest {
  // CRUD Operations
  create(tableName: string, data: any): Promise<ApiResponse<any>>;
  read(request: ReadRequest): Promise<ApiResponse<ReadResponse>>;
  update(tableName: string, data: any): Promise<ApiResponse<any>>;
  delete(tableName: string, id: string): Promise<ApiResponse<any>>;
  
  // Custom Actions
  action(actionName: string, data: any): Promise<ApiResponse<any>>;
  
  // Metadata
  metadata(tableName: string): Promise<ApiResponse<MetadataResponse>>;
  
  // Utilities
  setAuthToken(token: string): void;
  clearAuthToken(): void;
}
```

### Type Definitions

```tsx
interface ReadRequest {
  tableName: string;
  filters?: ReadFilter[];
  orderBy?: ReadOrderBy[];
  joins?: ReadJoin[];
  except?: ReadExcept[];
  fields?: string[];
  maxResults?: number;
  page?: number;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  validationErrors?: ValidationError[];
}
```

### Usage Examples

#### Create Operation
```tsx
const authRequest = useAuthRequest();

const response = await authRequest.create("Widget", {
  Name: "New Widget",
  Price: 19.99,
  CategoryId: "category-123"
});

if (response.success) {
  console.log("Created:", response.data.WidgetId);
}
```

#### Read Operation
```tsx
const response = await authRequest.read({
  tableName: "Widget",
  filters: [
    { field: "Price", operator: ">", value: 10 }
  ],
  orderBy: [
    { field: "Name", direction: "asc" }
  ],
  maxResults: 50,
  page: 1
});

if (response.success) {
  console.log("Records:", response.data.results);
  console.log("Total:", response.data.totalResults);
}
```

#### Update Operation
```tsx
const response = await authRequest.update("Widget", {
  WidgetId: "widget-123",
  Price: 24.99
});

if (response.success) {
  console.log("Updated successfully");
}
```

#### Delete Operation
```tsx
const response = await authRequest.delete("Widget", "widget-123");

if (response.success) {
  console.log("Deleted successfully");
}
```

#### Custom Action
```tsx
const response = await authRequest.action("ProcessWidget", {
  widgetId: "widget-123",
  operation: "validate"
});

if (response.success) {
  console.log("Action result:", response.data);
}
```

#### Get Metadata
```tsx
const response = await authRequest.metadata("Widget");

if (response.success) {
  const metadata = response.data;
  console.log("Fields:", metadata.fields);
  console.log("Primary Key:", metadata.primaryKey);
}
```

---

## Complete DataTable Example

```tsx
import { 
  DataTable, 
  useAuthRequest 
} from "@ixeta/xams";
import { Button } from "@mantine/core";
import { useRef } from "react";

function WidgetManager() {
  const tableRef = useRef<DataTableRef>();
  const authRequest = useAuthRequest();

  const handleExport = async () => {
    const records = tableRef.current?.getRecords() || [];
    // Export logic here
  };

  const handleBulkUpdate = async () => {
    const records = tableRef.current?.getRecords() || [];
    for (const record of records) {
      await authRequest.update("Widget", {
        ...record,
        Status: "processed"
      });
    }
    tableRef.current?.refresh();
  };

  return (
    <div className="h-full">
      <DataTable
        ref={tableRef}
        tableName="Widget"
        title="Widget Management"
        
        // Query configuration
        filters={[
          { field: "IsActive", operator: "=", value: true }
        ]}
        orderBy={[
          { field: "CreatedDate", direction: "desc" }
        ]}
        
        // Display configuration
        fields={[
          "Name",
          "Price",
          "Status",
          {
            header: "Stock",
            body: (record) => (
              <span className={record.Quantity < 10 ? "text-red-500" : ""}>
                {record.Quantity}
              </span>
            )
          }
        ]}
        columnWidths={["30%", "20%", "20%", "30%"]}
        
        // Features
        searchable={true}
        selectable="multiple"
        confirmDelete={true}
        refreshInterval={60000}
        
        // Form configuration
        formTitle="Edit Widget"
        formFields={["Name", "Price", "CategoryId", "Description"]}
        formCloseOnUpdate={true}
        
        // Events
        onRowClick={(record) => {
          console.log("Selected:", record);
          return true; // Open form
        }}
        formOnPostSave={(operation, record) => {
          if (operation === "CREATE") {
            console.log("New widget created:", record.WidgetId);
          }
        }}
        
        // Custom elements
        customCreateButton={(openForm) => (
          <div className="flex gap-2">
            <Button onClick={openForm}>Add Widget</Button>
            <Button variant="outline" onClick={handleExport}>Export</Button>
            <Button variant="light" onClick={handleBulkUpdate}>Bulk Update</Button>
          </div>
        )}
      />
    </div>
  );
}
```

---

← [Form Components](components-forms.md) | [Lookup Components](components-lookup.md) →