# @ixeta/xams - API Reference

**Version**: 1.0.16
**Description**: Xams React Components Library
**Package**: `@ixeta/xams`

---

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Context Providers](#context-providers)
5. [Core Hooks](#core-hooks)
6. [Core Components](#core-components)
7. [API Types](#api-types)
8. [Utilities](#utilities)
9. [Common Patterns](#common-patterns)

---

## Overview

The `@ixeta/xams` library provides a comprehensive set of React components and hooks for building data-driven applications with the Xams framework. It includes:

- **Data Management**: DataTable, DataGrid components for displaying and editing data
- **Form Building**: FormContainer, Field, SaveButton for creating forms
- **API Integration**: useAuthRequest hook for authenticated API calls
- **State Management**: useFormBuilder hook for form state and validation
- **Admin Tools**: AdminDashboard and related components
- **Type Safety**: Full TypeScript support with comprehensive type definitions

---

## Installation

```bash
npm install @ixeta/xams
```

### Peer Dependencies

The library requires the following peer dependencies:

```json
{
  "@mantine/core": "^8.3.0",
  "@mantine/dates": "^8.3.0",
  "@mantine/hooks": "^8.3.0",
  "@tabler/icons-react": "^3.34.1",
  "react": "^19.1.0",
  "react-dom": "^19.1.0",
  "zustand": "^4.4.0"
}
```

### CSS Imports

```tsx
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";
```

---

## Quick Start

### Basic Setup

```tsx
import {
  AuthContextProvider,
  AppContextProvider,
  useAuthRequest,
  DataTable,
  useFormBuilder,
  FormContainer,
  Field,
  SaveButton,
} from "@ixeta/xams";

function App() {
  return (
    <AuthContextProvider apiUrl="https://api.example.com">
      <AppContextProvider>
        <YourApp />
      </AppContextProvider>
    </AuthContextProvider>
  );
}
```

### Display Data with DataTable

```tsx
function WidgetList() {
  return <DataTable tableName="Widget" />;
}
```

### Create/Edit Forms with FormBuilder

```tsx
function WidgetForm({ widgetId }: { widgetId?: string }) {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
    id: widgetId,
  });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Field name="Name" />
      <Field name="Description" varient="textarea" />
      <Field name="Price" />
      <SaveButton />
    </FormContainer>
  );
}
```

### Make API Calls

```tsx
function MyComponent() {
  const authRequest = useAuthRequest();

  const loadData = async () => {
    const response = await authRequest.read({
      tableName: "Widget",
      filters: [{ field: "Active", operator: "eq", value: "true" }],
      orderBy: [{ field: "Name", order: "asc" }],
    });

    if (response.succeeded) {
      console.log(response.data.results);
    }
  };

  return <button onClick={loadData}>Load Widgets</button>;
}
```

---

## Context Providers

### AuthContextProvider

Provides authentication context for the application. **Required** for API calls.

```tsx
interface AuthContextProviderProps {
  apiUrl: string; // Base URL for API calls
  onUnauthorized?: () => void; // Callback when 401 received
  headers?: { [key: string]: string }; // Additional headers
  withCredentials?: boolean; // Include credentials in requests
  getAccessToken?: () => Promise<string | undefined>; // Custom token retrieval
  children?: any;
}

// Usage
<AuthContextProvider
  apiUrl="https://api.example.com"
  onUnauthorized={() => router.push("/login")}
>
  {children}
</AuthContextProvider>;
```

**Hook**: `useAuthContext()` - Access auth context values

---

### AppContextProvider

Provides application-level utilities. **Optional** but recommended for UI feedback.

```tsx
type AppContextShape = {
  showError: (message: string | React.ReactElement, title?: string) => void;
  showLoading: (text?: string) => void;
  hideLoading: () => void;
  showConfirm: (
    message: string,
    onOk: () => void,
    onCancel: () => void,
    title?: string
  ) => void;
  userId?: string | undefined;
  signalR: () => Promise<useSignalRResponse>;
  signalRState: string | undefined;
};

// Usage
<AppContextProvider>{children}</AppContextProvider>;
```

**Hook**: `useAppContext()` - Access app context utilities

---

## Core Hooks

### useAuthRequest

Primary hook for making authenticated API calls to the Xams backend.

```tsx
interface useAuthRequestProps {}

const authRequest = useAuthRequest(props?: useAuthRequestProps);
```

#### Methods

##### execute<T>(params: RequestParams): Promise<ApiResponse<T>>

Low-level method for custom requests.

```tsx
await authRequest.execute({
  method: "POST",
  url: "/custom/endpoint",
  body: { data: "value" },
});
```

##### config<T>(name: string): Promise<T | null>

Fetch configuration value by name.

```tsx
const config = await authRequest.config<string>("FeatureFlag");
```

##### whoAmI<T>(): Promise<ApiResponse<T>>

Get current user information.

```tsx
const user = await authRequest.whoAmI();
```

##### hasAllPermissions(permissions: string[]): Promise<boolean>

Check if user has all specified permissions.

```tsx
const canEdit = await authRequest.hasAllPermissions(["Widget.Update"]);
```

##### hasAnyPermissions(permissions: string[]): Promise<boolean>

Check if user has any of the specified permissions.

```tsx
const canAccess = await authRequest.hasAnyPermissions([
  "Admin.View",
  "User.View",
]);
```

##### tables(tag?: string): Promise<ApiResponse<TablesResponse[]>>

Get list of available tables, optionally filtered by tag.

```tsx
const tables = await authRequest.tables("app");
```

##### metadata(tableName: string): Promise<MetadataResponse>

Get metadata (fields, permissions, etc.) for a table.

```tsx
const metadata = await authRequest.metadata("Widget");
```

##### create<T>(tableName: string, fields: T, parameters?: any): Promise<ApiResponse<T>>

Create a new record.

```tsx
const response = await authRequest.create("Widget", {
  Name: "New Widget",
  Price: 19.99,
  Active: true,
});
```

##### read<T, U = any>(body: ReadRequest): Promise<ApiResponse<ReadResponse<T, U>>>

Query records with filters, joins, ordering, and pagination.

```tsx
const response = await authRequest.read({
  tableName: "Widget",
  fields: ["WidgetId", "Name", "Price"],
  filters: [
    { field: "Active", operator: "eq", value: "true" },
    { field: "Price", operator: "gte", value: "10" },
  ],
  orderBy: [{ field: "Name", order: "asc" }],
  page: 1,
  maxResults: 50,
});

if (response.succeeded) {
  const widgets = response.data.results;
  const totalPages = response.data.pages;
}
```

##### update<T>(tableName: string, fields: T, parameters?: any): Promise<ApiResponse<T>>

Update an existing record.

```tsx
const response = await authRequest.update("Widget", {
  WidgetId: "123e4567-e89b-12d3-a456-426614174000",
  Price: 24.99,
});
```

##### delete<T>(tableName: string, id: string, parameters?: any): Promise<ApiResponse<T>>

Delete a record by ID.

```tsx
const response = await authRequest.delete("Widget", widgetId);
```

##### upsert<T>(tableName: string, fields: T, parameters?: any): Promise<ApiResponse<T>>

Create or update a record (insert if new, update if exists).

```tsx
const response = await authRequest.upsert("Widget", {
  WidgetId: widgetId, // If exists, update; if not, create
  Name: "Updated Widget",
});
```

##### bulkCreate<T>(entities: T[], parameters?: any): Promise<ApiResponse<T>>

Create multiple records in a single operation.

```tsx
const response = await authRequest.bulkCreate([
  { Name: "Widget 1", Price: 10 },
  { Name: "Widget 2", Price: 20 },
]);
```

##### bulkUpdate<T>(entities: T[], parameters?: any): Promise<ApiResponse<T>>

Update multiple records in a single operation.

```tsx
const response = await authRequest.bulkUpdate([
  { WidgetId: id1, Price: 15 },
  { WidgetId: id2, Price: 25 },
]);
```

##### bulkDelete<T>(entities: T[], parameters?: any): Promise<ApiResponse<T>>

Delete multiple records in a single operation.

```tsx
const response = await authRequest.bulkDelete([
  { WidgetId: id1 },
  { WidgetId: id2 },
]);
```

##### bulkUpsert<T>(entities: T[], parameters?: any): Promise<ApiResponse<T>>

Upsert multiple records in a single operation.

##### bulk<T>(request: BulkRequest, parameters?: any): Promise<ApiResponse<T>>

Execute a complex bulk operation with mixed CRUD operations.

##### action<T>(actionName: string, parameters?: any, fileName?: string): Promise<ApiResponse<T>>

Execute a custom server action.

```tsx
const response = await authRequest.action("GenerateReport", {
  startDate: "2025-01-01",
  endDate: "2025-12-31",
});
```

##### file<T>(formData: FormData): Promise<ApiResponse<T>>

Upload a file.

```tsx
const formData = new FormData();
formData.append("file", fileInput.files[0]);
const response = await authRequest.file(formData);
```

---

### useFormBuilder

Hook for managing form state, validation, and submission.

```tsx
interface useFormBuilderProps {
  tableName: string; // Entity table name
  id?: string | null; // Record ID for editing (null for create)
  metadata?: MetadataResponse; // Pre-loaded metadata (optional)
  defaults?: FieldValue[]; // Default field values
  snapshot?: any; // Initial data snapshot
  lookupExclusions?: LookupExclusions[]; // Exclude specific lookup options
  lookupQueries?: LookupQuery[]; // Filter lookup options
  canUpdate?: boolean; // Permission to update
  canCreate?: boolean; // Permission to create
  onPreValidate?: PreSaveEvent; // Before validation
  onPreSave?: PreSaveEvent; // Before save (after validation)
  onPostSave?: PostSaveEvent; // After save
  forceShowLoading?: boolean; // Show loading state
  keepLoadingOnSuccess?: boolean; // Keep loading after success
}

const formBuilder = useFormBuilder(props);
```

#### Properties

```tsx
formBuilder.metadata; // MetadataResponse | undefined
formBuilder.data; // Current form data (T)
formBuilder.snapshot; // Original data snapshot (T)
formBuilder.lookupExclusions; // Lookup exclusions
formBuilder.lookupQueries; // Lookup queries
formBuilder.canUpdate; // Can update permission
formBuilder.canCreate; // Can create permission
formBuilder.canRead; // Can read permission { canRead: boolean, message: string }
formBuilder.defaults; // Default field values
formBuilder.validationMessages; // Validation errors
formBuilder.isLoading; // Is loading state
formBuilder.isSubmitted; // Has been submitted
formBuilder.operation; // "CREATE" | "UPDATE"
formBuilder.stateType; // State type string
formBuilder.tableName; // Table name
```

#### Methods

```tsx
// Set field value
formBuilder.setField(field: string, value: string | boolean | null | undefined | number): void

// Set field error
formBuilder.setFieldError(field: string, message: string): void

// Check if field or form is dirty
formBuilder.isDirty(field?: string): boolean

// Add a DataTable ref (for reloading child tables)
formBuilder.addDataTable(dataTable: DataTableRef): void

// Add required field dynamically
formBuilder.addRequiredField(fieldName: string): void

// Remove required field
formBuilder.removeRequiredField(fieldName: string): void

// Reload child data tables
formBuilder.reloadDataTables(): void

// Save form (triggers validation and API call)
formBuilder.save(preValidate?, preSaveEvent?, postSaveEvent?): Promise<void>

// Save without UI feedback
formBuilder.saveSilent(parameters?: any): Promise<any>

// Load record by ID
formBuilder.load(id: string, setForceShowLoading?: boolean): Promise<void>

// Set snapshot (initial data)
formBuilder.setSnapshot(snapshot: T, forceShowLoading?: boolean): Promise<void>

// Reload current record
formBuilder.reload(reloadDataTables?: boolean): Promise<void>

// Clear edits (reset to snapshot)
formBuilder.clearEdits(): void

// Clear form completely
formBuilder.clear(): void

// Validate form
formBuilder.validate(): boolean

// Set loading state
formBuilder.setShowForceLoading(loading: boolean): void
```

#### Type Helpers

```tsx
export type useFormBuilderType<T = any> = ReturnType<typeof useFormBuilder<T>>;

export type SaveEventResponse = {
  continue: boolean; // Continue with save?
  parameters?: any; // Additional parameters to pass
};

export type PreSaveEvent = (submissionData: any) => Promise<SaveEventResponse>;
export type PostSaveEvent = (
  operation: "CREATE" | "UPDATE" | "FAILED",
  id: string,
  data: any
) => void;
```

---

### useColor

Hook for accessing Mantine color values.

```tsx
const color = useColor();
const primaryColor = color.primaryColor;
```

---

## Core Components

### DataTable

Displays data in a table with built-in CRUD operations, searching, sorting, and pagination.

```tsx
interface DataTableProps {
  // Basic Configuration
  tableName: string; // Entity table name (REQUIRED)
  title?: string; // Table title
  disabledMessage?: string; // Message when disabled
  confirmDelete?: boolean; // Confirm before delete
  maxResults?: number; // Records per page (default: 50)

  // Display Fields
  fields?: DataTableField[]; // Fields to display (default: all)
  additionalFields?: string[]; // Extra fields to fetch (not displayed)
  columnWidths?: string[]; // Column width overrides

  // Data Query
  orderBy?: ReadOrderBy[]; // Default sorting
  filters?: ReadFilter[]; // Default filters
  joins?: ReadJoin[]; // Join related tables
  except?: ReadExcept[]; // Exclude records

  // Features
  scrollable?: boolean; // Enable horizontal scroll
  searchable?: boolean; // Enable search box
  selectable?: "single" | "multiple"; // Row selection mode
  showActiveSwitch?: boolean; // Show active/inactive toggle
  showOptions?: boolean; // Show options menu
  pagination?: boolean; // Enable pagination

  // Permissions
  canDeactivate?: boolean; // Allow deactivate
  canDelete?: boolean; // Allow delete
  canUpdate?: boolean; // Allow update
  canCreate?: boolean; // Allow create
  canImport?: boolean; // Allow import
  canExport?: boolean; // Allow export

  // Form Configuration
  formTitle?: string; // Form modal title
  formFields?: string[]; // Fields in form (default: all)
  formFieldDefaults?: FieldValue[]; // Default form values
  formLookupExclusions?: LookupExclusions[];
  formLookupQueries?: LookupQuery[];
  formCloseOnCreate?: boolean; // Close form after create
  formCloseOnUpdate?: boolean; // Close form after update
  formCloseOnEscape?: boolean; // Close form on ESC
  formClassNames?: string; // Form CSS classes
  formZIndex?: number; // Form modal z-index
  formMaxWidth?: number; // Form max width
  formMinWidth?: number; // Form min width
  formFullScreen?: boolean; // Fullscreen form
  formHideSaveButton?: boolean; // Hide save button

  // Custom Rendering
  customForm?: (formbuilder, disclosure) => React.ReactNode;
  appendCustomForm?: (formbuilder) => React.ReactNode;
  formAppendButton?: (formbuilder) => React.ReactNode;
  customCreateButton?: (openForm) => React.ReactNode;
  customRow?: (record) => React.ReactNode;

  // Callbacks
  onInitialLoad?: (results: any[]) => void;
  onPostDelete?: (record: any) => void;
  onRowClick?: (record: any) => boolean; // Return false to prevent default
  onPageChange?: (page: number) => void;
  onDataLoaded?: (data: ReadResponse<any>) => void;
  formOnClose?: () => void;
  formOnOpen?: (operation: "CREATE" | "UPDATE", record: any) => void;
  formOnPreSave?: (submissionData: any, parameters?: any) => void;
  formOnPostSave?: (operation: "CREATE" | "UPDATE", record: any) => void;
  deleteConfirmation?: (record: any) => Promise<{
    title?: string;
    message?: string;
    showPrompt?: boolean;
  }>;

  // Advanced
  refreshInterval?: number; // Auto-refresh interval (ms)
  tableStyle?: TableStyle; // Table styling options
}

// Custom Field Definition
type DataTableCustomField = {
  header: string | ((refHandle: DataTableRef) => React.ReactNode);
  body: (record: any, refHandle: DataTableRef) => React.ReactNode;
};

type DataTableField = string | DataTableCustomField;
```

#### DataTable Ref Methods

```tsx
const dataTableRef = useRef<DataTableRef>(null);

<DataTable ref={dataTableRef} tableName="Widget" />;

// Available methods:
dataTableRef.current.refresh(); // Refresh data
dataTableRef.current.openForm(recordData); // Open form
dataTableRef.current.getRecords(); // Get current records
dataTableRef.current.setRecords(fn); // Update records
dataTableRef.current.showLoading(); // Show loading
dataTableRef.current.sort(field); // Sort by field
dataTableRef.current.Metadata; // Access metadata
```

#### Examples

**Basic DataTable**:

```tsx
<DataTable tableName="Widget" />
```

**With Filters and Sorting**:

```tsx
<DataTable
  tableName="Widget"
  filters={[{ field: "Active", operator: "==", value: "true" }]}
  orderBy={[{ field: "CreatedOn", order: "desc" }]}
  maxResults={25}
/>
```

**Custom Fields**:

```tsx
<DataTable
  tableName="Widget"
  fields={[
    "Name",
    "Price",
    {
      header: "Actions",
      body: (record, refHandle) => (
        <button onClick={() => doSomething(record)}>Action</button>
      ),
    },
  ]}
/>
```

**With Form Customization**:

```tsx
<DataTable
  tableName="Widget"
  formFields={["Name", "Description", "Price"]}
  formFieldDefaults={[{ name: "Active", value: true }]}
  formOnPostSave={(operation, record) => {
    console.log(`${operation} completed for`, record);
  }}
/>
```

---

### DataTableSelectable

DataTable with row selection capabilities.

```tsx
<DataTableSelectable
  tableName="Widget"
  selectable="multiple"
  // ... other DataTable props
/>
```

---

### DataGrid

Editable grid component for bulk data entry.

```tsx
interface DataGridProps {
  tableName: string;
  // ... extensive props similar to DataTable
}

const dataGridRef = useRef<DataGridRef>(null);

<DataGrid ref={dataGridRef} tableName="Widget" />;
```

---

### FormContainer

Container component for forms built with useFormBuilder.

```tsx
interface FormContainerProps {
  formBuilder: useFormBuilderType; // FormBuilder instance (REQUIRED)
  className?: string; // CSS classes
  children?: any; // Form fields
  showLoading?: boolean; // Override loading state
  onPreValidate?: PreSaveEvent; // Before validation
  onPreSave?: PreSaveEvent; // Before save
  onPostSave?: PostSaveEvent; // After save
}

// Usage
<FormContainer formBuilder={formBuilder}>
  <Field name="Name" />
  <SaveButton />
</FormContainer>;
```

---

### Field

Form field component that automatically renders the appropriate input based on field metadata.

```tsx
interface FieldProps {
  name: string;                          // Field name (REQUIRED)
  label?: string | React.ReactNode;      // Custom label
  focus?: boolean;                       // Auto-focus
  varient?: "rich" | "textarea";         // Input variant
  placeholder?: string;                  // Placeholder text
  dateInput?: DateInputProps;            // Date input config (Mantine)
  onChange?: (value: string | boolean | null | undefined, data?: string | null) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;                    // Override required
  allowNegative?: boolean;               // For number fields
  size?: MantineSize;                    // Field size
}

// Usage
<Field name="Name" />
<Field name="Description" varient="textarea" />
<Field name="Price" allowNegative={false} />
<Field name="BirthDate" />
<Field name="Notes" varient="rich" />  // Rich text editor
```

---

### SaveButton

Submit button for forms. Automatically triggers form validation and save.

```tsx
interface SaveButtonProps {
  label?: string;                        // Button text (default: "Save")
  varient?: "filled" | "outline" | "light" | "white" | "default" | "subtle" | "gradient";
  className?: string;                    // CSS classes
  size?: MantineSize;                    // Button size
}

// Usage
<SaveButton />
<SaveButton label="Submit" varient="filled" size="lg" />
```

---

### ToggleMode

Component for toggling between light/dark mode or other binary states.

```tsx
<ToggleMode />
```

---

### AdminDashboard

Full-featured admin dashboard component.

```tsx
interface AdminDashboardProps {
  children?: any;
  navItems?: NavItem[];
  // ... additional props
}

<AdminDashboard />;
```

---

## API Types

### ApiResponse<T>

Standard response wrapper for all API calls.

```tsx
interface ApiResponse<T> {
  succeeded: boolean; // Operation success status
  data: T; // Response data
  friendlyMessage: string; // User-friendly message
  logMessage: string; // Technical/debug message
  response: Response | undefined; // Original fetch Response
}
```

---

### ReadRequest

Request structure for querying data.

```tsx
interface ReadRequest {
  tableName: string; // Table to query (REQUIRED)
  fields?: string[]; // Fields to return
  id?: string; // Single record ID
  orderBy?: ReadOrderBy[]; // Sorting
  maxResults?: number; // Page size
  page?: number; // Page number (1-based)
  filters?: ReadFilter[]; // Filters
  joins?: ReadJoin[]; // Joins
  except?: ReadExcept[]; // Exclusions
  distinct?: boolean; // Distinct records
  denormalize?: boolean; // Denormalize lookups
  parameters?: any; // Custom parameters
}
```

---

### ReadFilter

Filter criteria for queries.

```tsx
interface ReadFilter {
  logicalOperator?: "AND" | "OR"; // For nested filters
  filters?: ReadFilter[]; // Nested filters
  field?: string; // Field name
  value?: string | null; // Filter value
  operator?: "==" | "!=" | ">" | ">=" | "<" | "<=" | "contains"; // Comparison operator
}
```

**Examples**:

```tsx
// Simple filter
{ field: "Active", operator: "==", value: "true" }

// Multiple filters (AND by default)
filters: [
  { field: "Active", operator: "==", value: "true" },
  { field: "Price", operator: ">=", value: "10" }
]

// Complex filter (OR)
{
  logicalOperator: "OR",
  filters: [
    { field: "Status", operator: "==", value: "Active" },
    { field: "Status", operator: "==", value: "Pending" }
  ]
}

// Nested filters
{
  logicalOperator: "AND",
  filters: [
    { field: "Active", operator: "==", value: "true" },
    {
      logicalOperator: "OR",
      filters: [
        { field: "Category", operator: "==", value: "A" },
        { field: "Category", operator: "==", value: "B" }
      ]
    }
  ]
}
```

---

### ReadOrderBy

Sorting specification.

```tsx
interface ReadOrderBy {
  field: string; // Field to sort by
  order?: string; // "asc" | "desc" (default: "asc")
}

// Example
orderBy: [
  { field: "Name", order: "asc" },
  { field: "CreatedOn", order: "desc" },
];
```

---

### ReadJoin

Join related tables.

```tsx
interface ReadJoin {
  fields: string[];            // Fields from joined table
  alias?: string;              // Table alias
  fromTable: string;           // Source table
  fromField: string;           // Source field
  toTable: string;             // Target table
  toField: string;             // Target field
  filters?: ReadFilter[];      // Filters on joined table
}

// Example: Join Widget to WidgetType
{
  fromTable: "Widget",
  fromField: "WidgetTypeId",
  toTable: "Option",
  toField: "OptionId",
  fields: ["Name"],
  filters: [{ field: "OptionGroup", operator: "==", value: "WidgetType" }]
}
```

---

### ReadExcept

Exclude records that match a subquery.

```tsx
interface ReadExcept {
  fromField: string;           // Field to compare
  query: ReadRequest;          // Subquery
}

// Example: Exclude widgets that have orders
{
  fromField: "WidgetId",
  query: {
    tableName: "Order",
    fields: ["WidgetId"]
  }
}
```

---

### ReadResponse<T, U>

Response structure for read operations.

```tsx
interface ReadResponse<T, U = any> {
  pages: number; // Total pages
  currentPage: number; // Current page number
  totalResults: number; // Total record count
  maxResults: number; // Records per page
  tableName: string; // Queried table
  orderBy?: ReadOrderBy[]; // Applied sorting
  results: T[]; // Result records
  parameters: U; // Custom parameters returned
}
```

---

### MetadataResponse

Entity metadata including fields, permissions, and relationships.

```tsx
interface MetadataResponse {
  tableName: string;
  displayName: string;
  fields: MetadataField[];
  permissions: Permission[];
  // ... additional metadata
}

interface MetadataField {
  name: string;
  displayName: string;
  dataType: string;
  isRequired: boolean;
  isReadOnly: boolean;
  // ... field properties
}
```

---

### TablesResponse

Available table information.

```tsx
interface TablesResponse {
  tableName: string;
  displayName: string;
  tag?: string;
  // ... table properties
}
```

---

### BulkRequest

Complex bulk operations with mixed CRUD.

```tsx
interface BulkRequest {
  creates?: any[];
  updates?: any[];
  deletes?: any[];
  tableName?: string;
  // ... bulk configuration
}
```

---

## Utilities

### Query Helpers

```tsx
import { Query } from "@ixeta/xams";

// Build filters programmatically
const filter = Query.eq("Active", true);
const filter2 = Query.gte("Price", 10);
const combined = Query.and([filter, filter2]);
```

### URL Helpers

```tsx
import { getQueryParam, getUserIdUrlPart } from "@ixeta/xams";

// Get query parameter
const userId = getQueryParam("userid");

// Add userId to URL
const urlPart = getUserIdUrlPart(userId);
```

### Permission Store

```tsx
import { usePermissionStore } from "@ixeta/xams";

const { permissions, setPermissions, hasPermission } = usePermissionStore();
```

---

## Common Patterns

### Pattern 1: Master-Detail View

```tsx
function MasterDetail() {
  const [selectedId, setSelectedId] = useState<string | null>(null);

  return (
    <div>
      <DataTable
        tableName="Widget"
        onRowClick={(record) => {
          setSelectedId(record.WidgetId);
          return false; // Prevent default form open
        }}
      />

      {selectedId && (
        <div>
          <h2>Details</h2>
          <DataTable
            tableName="WidgetDetail"
            filters={[{ field: "WidgetId", operator: "==", value: selectedId }]}
          />
        </div>
      )}
    </div>
  );
}
```

---

### Pattern 2: Custom Form with Validation

```tsx
function CustomForm() {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
    onPreValidate: async (data) => {
      // Custom validation
      if (data.Price < 0) {
        formBuilder.setFieldError("Price", "Price cannot be negative");
        return { continue: false };
      }
      return { continue: true };
    },
    onPostSave: (operation, id, data) => {
      console.log(`${operation} completed`, data);
      // Navigate or refresh
    },
  });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Field name="Name" required />
      <Field name="Description" varient="textarea" />
      <Field name="Price" allowNegative={false} />

      {formBuilder.isDirty() && <p>You have unsaved changes</p>}

      <SaveButton />
    </FormContainer>
  );
}
```

---

### Pattern 3: Filtered DataTable with Custom Actions

```tsx
function FilteredTable() {
  const [activeOnly, setActiveOnly] = useState(true);
  const tableRef = useRef<DataTableRef>(null);

  return (
    <div>
      <label>
        <input
          type="checkbox"
          checked={activeOnly}
          onChange={(e) => setActiveOnly(e.target.checked)}
        />
        Active Only
      </label>

      <DataTable
        ref={tableRef}
        tableName="Widget"
        filters={
          activeOnly ? [{ field: "Active", operator: "eq", value: "true" }] : []
        }
        fields={[
          "Name",
          "Price",
          {
            header: "Actions",
            body: (record, refHandle) => (
              <button
                onClick={async () => {
                  // Custom action
                  await doSomething(record.WidgetId);
                  refHandle.refresh();
                }}
              >
                Process
              </button>
            ),
          },
        ]}
      />
    </div>
  );
}
```

---

### Pattern 4: Bulk Operations

```tsx
function BulkOperations() {
  const authRequest = useAuthRequest();

  const processBulk = async () => {
    const updates = [
      { WidgetId: id1, Status: "Processed" },
      { WidgetId: id2, Status: "Processed" },
    ];

    const response = await authRequest.bulkUpdate(updates);

    if (response.succeeded) {
      console.log("Bulk update successful");
    }
  };

  return <button onClick={processBulk}>Process Selected</button>;
}
```

---

### Pattern 5: Related Data with Joins

```tsx
function WidgetWithCategory() {
  return (
    <DataTable
      tableName="Widget"
      fields={[
        "Name",
        "Price",
        "Category.Name", // Joined field
      ]}
      joins={[
        {
          fromTable: "Widget",
          fromField: "CategoryId",
          toTable: "Option",
          toField: "OptionId",
          alias: "Category",
          fields: ["Name"],
          filters: [
            { field: "OptionGroup", operator: "eq", value: "Category" },
          ],
        },
      ]}
    />
  );
}
```

---

### Pattern 6: Conditional Rendering Based on Permissions

```tsx
function ConditionalActions() {
  const authRequest = useAuthRequest();
  const [canEdit, setCanEdit] = useState(false);

  useEffect(() => {
    authRequest.hasAllPermissions(["Widget.Update"]).then(setCanEdit);
  }, []);

  return (
    <DataTable
      tableName="Widget"
      canUpdate={canEdit}
      canDelete={canEdit}
      canCreate={canEdit}
    />
  );
}
```

---

### Pattern 7: Form with Dependent Fields

```tsx
function DependentFields() {
  const formBuilder = useFormBuilder({ tableName: "Widget" });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Field
        name="Category"
        onChange={(value) => {
          // Clear dependent field when category changes
          formBuilder.setField("Subcategory", null);
        }}
      />

      <Field name="Subcategory" disabled={!formBuilder.data.Category} />

      <SaveButton />
    </FormContainer>
  );
}
```

---

## Best Practices

1. **Always wrap your app with AuthContextProvider** - Required for API calls
2. **Use AppContextProvider for user feedback** - Shows errors, loading states, confirmations
3. **Leverage metadata** - Field metadata drives automatic form rendering
4. **Use TypeScript** - Full type safety with generics
5. **Handle permissions** - Check permissions before showing UI elements
6. **Validate early** - Use onPreValidate for custom validation
7. **Optimize queries** - Use fields parameter to fetch only needed data
8. **Use refs for imperative operations** - Access DataTable/DataGrid methods via refs
9. **Implement error handling** - Check `response.succeeded` before using data
10. **Use bulk operations for multiple records** - More efficient than individual calls

---

## API URLs

The following API endpoints are used internally:

```tsx
export const API_DATA_PERMISSIONS = "/xams/permissions";
export const API_DATA_ACTION = "/xams/action";
export const API_DATA_CREATE = "/xams/create";
export const API_DATA_READ = "/xams/read";
export const API_DATA_UPDATE = "/xams/update";
export const API_DATA_DELETE = "/xams/delete";
export const API_DATA_FILE = "/xams/file";
export const API_DATA_METADATA = "/xams/metadata";
export const API_CONFIG = "/config";
```

---

## Type Exports

All TypeScript types are exported and available for use:

```tsx
import type {
  // Hooks
  useAuthRequestType,
  useFormBuilderType,

  // API
  ReadFilter,
  ReadRequest,
  ReadResponse,
  ReadOrderBy,
  ReadJoin,
  ReadExcept,
  ApiResponse,
  TablesResponse,
  MetadataResponse,

  // Components
  DataTableProps,
  DataTableRef,
  DataGridRef,

  // Admin
  AdminDashboardProps,
  NavItem,
  RolePermissionState,
} from "@ixeta/xams";
```

---

## Support

For issues, questions, or contributions, please visit:

- **Documentation**: https://xams.io
- **GitHub**: https://github.com/ixeta/xams
- **Email**: support@ixeta.net

---

**Last Updated**: 2025-10-12
**Version**: 1.0.16
