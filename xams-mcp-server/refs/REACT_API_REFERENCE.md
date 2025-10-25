# @ixeta/xams - API Reference

**Version**: 1.0.16
**Description**: Xams React API Library
**Package**: `@ixeta/xams`

---

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Context Providers](#context-providers)
4. [Core Hooks](#core-hooks)
5. [API Types](#api-types)
6. [Utilities](#utilities)
7. [Components](#components)
8. [Common Patterns](#common-patterns)
9. [Best Practices](#best-practices)

---

## Overview

The `@ixeta/xams` library provides React hooks and utilities for building data-driven applications with the Xams framework. It includes:

- **API Integration**: useAuthRequest hook for authenticated API calls
- **Query Building**: Query class for fluent query construction
- **State Management**: Context providers for auth and app utilities
- **Admin Tools**: AdminDashboard component
- **Type Safety**: Full TypeScript support with comprehensive type definitions

---

## Installation

```bash
npm install @ixeta/xams
```

### Peer Dependencies

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

## Context Providers

### AuthContextProvider

Provides authentication context for the application. **Required** for API calls.

```tsx
interface AuthContextProviderProps {
  apiUrl: string;                                   // Base URL for API (REQUIRED)
  onUnauthorized?: () => void;                      // Callback when 401 received
  headers?: { [key: string]: string };              // Additional headers
  withCredentials?: boolean;                        // Include credentials
  getAccessToken?: () => Promise<string | undefined>; // Custom token retrieval
  children?: any;
}

// Usage
<AuthContextProvider
  apiUrl="https://api.example.com"
  headers={{ UserId: userId }}
  onUnauthorized={() => router.push("/login")}
  getAccessToken={async () => await getFirebaseToken()}
>
  {children}
</AuthContextProvider>
```

**Hook**: `useAuthContext()` - Access auth context values

---

### AppContextProvider

Provides application-level utilities. **Recommended** for UI feedback.

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
  signalR: () => Promise<SignalRConnection>;
  signalRState: string | undefined;
};

// Usage
<AppContextProvider>{children}</AppContextProvider>
```

**Hook**: `useAppContext()` - Access app utilities

---

## Core Hooks

### useAuthRequest

Primary hook for making authenticated API calls to the Xams backend.

```tsx
const authRequest = useAuthRequest();
```

#### Methods

##### create<T>(tableName: string, fields: T, parameters?: any): Promise<ApiResponse<T>>

Create a new record.

```tsx
const response = await authRequest.create("Widget", {
  Name: "New Widget",
  Price: 19.99,
  Active: true,
});

if (response.succeeded) {
  const createdWidget = response.data;
}
```

##### read<T, U = any>(body: ReadRequest): Promise<ApiResponse<ReadResponse<T, U>>>

Query records with filters, joins, ordering, and pagination.

```tsx
// Using ReadRequest object
const response = await authRequest.read<Widget>({
  tableName: "Widget",
  fields: ["WidgetId", "Name", "Price"],
  filters: [
    { field: "Active", operator: "==", value: "true" },
    { field: "Price", operator: ">=", value: "10" },
  ],
  orderBy: [{ field: "Name", order: "asc" }],
  page: 1,
  maxResults: 50,
});

// Using Query builder (recommended)
import { Query } from "@ixeta/xams";
const query = new Query(["WidgetId", "Name", "Price"])
  .from("Widget")
  .where("Active", "==", "true")
  .and("Price", ">=", "10")
  .orderBy("Name", "asc")
  .top(50)
  .toReadRequest();
const response = await authRequest.read<Widget>(query);

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
  Price: 29.99,
});
```

##### bulkCreate<T>(entities: T[], parameters?: any): Promise<ApiResponse<T>>

Create multiple records in a single operation.

```tsx
const response = await authRequest.bulkCreate([
  { Name: "Widget 1", Price: 10 },
  { Name: "Widget 2", Price: 20 },
  { Name: "Widget 3", Price: 30 },
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

```tsx
const response = await authRequest.bulk({
  creates: [{ Name: "New" }],
  updates: [{ WidgetId: id, Price: 99 }],
  deletes: [{ WidgetId: id2 }],
});
```

##### action<T>(actionName: string, parameters?: any, fileName?: string): Promise<ApiResponse<T>>

Execute a custom server action.

```tsx
const response = await authRequest.action("GenerateReport", {
  startDate: "2025-01-01",
  endDate: "2025-12-31",
});

// File download
const response = await authRequest.action(
  "ExportData",
  { format: "excel" },
  "report.xlsx" // Triggers browser download
);
```

##### file<T>(formData: FormData): Promise<ApiResponse<T>>

Upload a file.

```tsx
const formData = new FormData();
formData.append("file", fileInput.files[0]);
formData.append("name", "UploadDocument");
formData.append("parameters", JSON.stringify({ category: "invoice" }));
const response = await authRequest.file(formData);
```

##### metadata(tableName: string): Promise<MetadataResponse>

Get metadata (fields, permissions, etc.) for a table.

```tsx
const metadata = await authRequest.metadata("Widget");

// Access field information
metadata.fields.forEach((field) => {
  console.log(field.name, field.type, field.isRequired);
});
```

##### tables(tag?: string): Promise<ApiResponse<TablesResponse[]>>

Get list of available tables, optionally filtered by tag.

```tsx
const allTables = await authRequest.tables();
const appTables = await authRequest.tables("app");
```

##### whoAmI<T>(): Promise<ApiResponse<T>>

Get current user information.

```tsx
const response = await authRequest.whoAmI();
if (response.succeeded) {
  const user = response.data;
}
```

##### hasAllPermissions(permissions: string[]): Promise<boolean>

Check if user has all specified permissions.

```tsx
const canEdit = await authRequest.hasAllPermissions([
  "TABLE_Widget_UPDATE_SYSTEM",
]);
```

##### hasAnyPermissions(permissions: string[]): Promise<boolean>

Check if user has any of the specified permissions.

```tsx
const canView = await authRequest.hasAnyPermissions([
  "TABLE_Widget_READ_USER",
  "TABLE_Widget_READ_TEAM",
  "TABLE_Widget_READ_SYSTEM",
]);
```

##### config<T>(name: string): Promise<T | null>

Fetch configuration value by name.

```tsx
const featureFlag = await authRequest.config<boolean>("NewFeatureEnabled");
```

##### execute<T>(params: RequestParams): Promise<ApiResponse<T>>

Low-level method for custom requests.

```tsx
await authRequest.execute({
  method: "POST",
  url: "/custom/endpoint",
  body: { data: "value" },
});
```

---

## API Types

### ApiResponse<T>

Standard response wrapper for all API calls.

```tsx
interface ApiResponse<T> {
  succeeded: boolean;        // Operation success status
  data: T;                   // Response data
  friendlyMessage: string;   // User-friendly message
  logMessage: string;        // Technical/debug message
  response: Response | undefined; // Original fetch Response
}

// Always check succeeded before using data
if (response.succeeded) {
  const widget = response.data;
} else {
  console.error(response.friendlyMessage);
}
```

---

### ReadRequest

Request structure for querying data.

```tsx
interface ReadRequest {
  tableName: string;          // Table to query (REQUIRED)
  fields?: string[];          // Fields to return
  id?: string;                // Single record ID
  orderBy?: ReadOrderBy[];    // Sorting
  maxResults?: number;        // Page size
  page?: number;              // Page number (1-based)
  filters?: ReadFilter[];     // Filters
  joins?: ReadJoin[];         // Joins
  except?: ReadExcept[];      // Exclusions
  distinct?: boolean;         // Distinct records
  denormalize?: boolean;      // Denormalize lookups
  parameters?: any;           // Custom parameters
}
```

---

### ReadFilter

Filter criteria for queries.

```tsx
interface ReadFilter {
  logicalOperator?: "AND" | "OR"; // For nested filters
  filters?: ReadFilter[];         // Nested filters
  field?: string;                 // Field name
  value?: string | null;          // Filter value
  operator?: "==" | "!=" | ">" | ">=" | "<" | "<=" | "contains";
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
  field: string;    // Field to sort by
  order?: string;   // "asc" | "desc" (default: "asc")
}

// Example
orderBy: [
  { field: "Name", order: "asc" },
  { field: "CreatedDate", order: "desc" },
];
```

---

### ReadJoin

Join related tables.

```tsx
interface ReadJoin {
  fromTable: string;       // Source table
  fromField: string;       // Source field
  toTable: string;         // Target table
  toField: string;         // Target field
  alias?: string;          // Table alias
  fields: string[];        // Fields from joined table
  filters?: ReadFilter[];  // Filters on joined table
  type?: string;           // "inner" | "left"
}

// Example: Join Widget to Category Option
{
  fromTable: "Widget",
  fromField: "CategoryId",
  toTable: "Option",
  toField: "OptionId",
  alias: "Category",
  fields: ["Label", "Value"],
  filters: [{ field: "Name", operator: "==", value: "WidgetCategory" }]
}
```

---

### ReadExcept

Exclude records that match a subquery.

```tsx
interface ReadExcept {
  fromField: string;    // Field to compare
  query: ReadRequest;   // Subquery
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
  pages: number;           // Total pages
  currentPage: number;     // Current page number
  totalResults: number;    // Total record count
  maxResults: number;      // Records per page
  tableName: string;       // Queried table
  orderBy?: ReadOrderBy[]; // Applied sorting
  results: T[];            // Result records
  parameters: U;           // Custom parameters returned
  distinct?: boolean;
  denormalize?: boolean;
}
```

---

### MetadataResponse

Entity metadata including fields, permissions, and relationships.

```tsx
interface MetadataResponse {
  tableName: string;
  displayName: string;
  primaryKey: string;
  fields: MetadataField[];
}

interface MetadataField {
  name: string;
  displayName: string;
  type: string;                // "string" | "number" | "boolean" | "datetime" | "lookup"
  isRequired: boolean;
  isReadOnly: boolean;
  isRecommended: boolean;
  isNullable: boolean;
  lookupTable?: string;
  lookupTableNameField?: string;
  lookupTablePrimaryKeyField?: string;
  option?: string;            // Option group name
  dateFormat?: string;        // Day.js format
  characterLimit?: number;
  numberRange?: string;
  order: number;
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
  upserts?: any[];
  tableName?: string;
}

// Example
const response = await authRequest.bulk({
  creates: [{ Name: "New 1" }, { Name: "New 2" }],
  updates: [{ WidgetId: id1, Price: 15 }],
  deletes: [{ WidgetId: id2 }],
});
```

---

## Utilities

### Query Class

Fluent API for building ReadRequest objects.

```tsx
import { Query } from "@ixeta/xams";

// Constructor
new Query(fields: string[])

// Methods (chainable)
.from(tableName: string): Query
.where(field: string, operator: string, value: any): Query
.and(field: string, operator: string, value: any): Query
.or(field: string, operator: string, value: any): Query
.join(fromField: string, toField: string, alias: string, fields: string[]): Query
.leftJoin(fromField: string, toField: string, alias: string, fields: string[]): Query
.orderBy(field: string, order: "asc" | "desc"): Query
.top(count: number): Query
.page(pageNumber: number): Query
.distinct(): Query
.denormalize(): Query
.toReadRequest(): ReadRequest  // Terminal method
```

**Examples**:

```tsx
// Basic query
const query = new Query(["*"]).from("Widget").toReadRequest();

// With filters
const query = new Query(["Name", "Price"])
  .from("Widget")
  .where("Active", "==", "true")
  .and("Price", ">", "10")
  .or("Featured", "==", "true")
  .toReadRequest();

// With joins
const query = new Query(["*"])
  .from("Account")
  .join("Account.AccountId", "Contact.AccountId", "c", ["FirstName", "LastName"])
  .where("c.FirstName", "Contains", "John")
  .orderBy("c.LastName", "asc")
  .toReadRequest();

// Cascading joins
const query = new Query(["*"])
  .from("Account")
  .join("Account.AccountId", "Contact.AccountId", "c", ["*"])
  .join("c.AddressId", "Address.AddressId", "a", ["City", "State"])
  .toReadRequest();

// With paging
const query = new Query(["*"])
  .from("Widget")
  .orderBy("CreatedDate", "desc")
  .top(25)
  .page(2)
  .toReadRequest();

// Denormalized (joins returned as arrays)
const query = new Query(["AccountId", "Name"])
  .from("Account")
  .join("Account.AccountId", "Contact.AccountId", "c", ["*"])
  .denormalize()
  .toReadRequest();

// Result structure when denormalized:
// {
//   AccountId: "...",
//   Name: "MegaCorp",
//   c: [
//     { ContactId: "...", FirstName: "John", LastName: "Smith" },
//     { ContactId: "...", FirstName: "Jane", LastName: "Doe" }
//   ]
// }
```

---

### URL Helpers

```tsx
import { getQueryParam, getUserIdUrlPart, addUserIdUrlParam } from "@ixeta/xams";

// Get query parameter from URL
const userId = getQueryParam("userid", router.asPath);
const page = getQueryParam("page", window.location.href);

// Add userId to navigation (maintains userid query param)
router.push(getUserIdUrlPart(router.asPath, "/project"));

// Add userId from current URL to destination URL
const url = addUserIdUrlParam(router.asPath, "/dashboard");
```

---

### Permission Store

```tsx
import { usePermissionStore } from "@ixeta/xams";

const { permissions, setPermissions, hasPermission } = usePermissionStore();

// Check if user has permission
if (hasPermission("TABLE_Widget_CREATE_SYSTEM")) {
  // Show create button
}
```

---

## Components

### FieldRichText

Rich text editor component for text fields requiring formatted content.

```tsx
import { FieldRichText } from "@ixeta/xams";

// Usage
<FieldRichText name="Notes" />
```

**Note**: The `variant="rich"` prop on the `Field` component is deprecated and no longer works. Use `FieldRichText` instead.

```tsx
// ❌ DEPRECATED - No longer works
<Field name="Notes" variant="rich" />

// ✅ REQUIRED - Use FieldRichText component
<FieldRichText name="Notes" />
```

---

### AdminDashboard

Full-featured admin dashboard component for managing entities, users, roles, and permissions.

```tsx
interface AdminDashboardProps {
  title?: string;
  visibleEntities?: string[];           // Only show these entities
  hiddenEntities?: string[];            // Hide these entities
  showEntityDisplayNames?: boolean;     // Show friendly names
  addMenuItems?: NavItem[];             // Custom menu items
  hiddenMenuItems?: string[];           // Hide default menu items
  forceHideImportData?: boolean;
  forceHideExportData?: boolean;
  forceHideToggleMode?: boolean;
  userCard?: ReactNode;
  accessDeniedMessage?: ReactNode;
}

interface NavItem {
  order: number;
  navLink: React.JSX.Element;
}

// Usage
import { AdminDashboard } from "@ixeta/xams";
import { NavLink } from "@mantine/core";
import { IconLogout, IconSettings } from "@tabler/icons-react";

function Admin() {
  return (
    <AdminDashboard
      title="My Admin Panel"
      visibleEntities={["Widget", "Order", "Customer"]}
      addMenuItems={[
        {
          order: 100,
          navLink: (
            <NavLink
              label="Settings"
              leftSection={<IconSettings size={16} />}
              onClick={() => router.push("/settings")}
            />
          ),
        },
        {
          order: 10000,
          navLink: (
            <NavLink
              label="Logout"
              leftSection={<IconLogout size={16} />}
              onClick={() => handleLogout()}
            />
          ),
        },
      ]}
    />
  );
}
```

---

## Common Patterns

### Pattern 1: List with Mantine Table

```tsx
import { useAuthRequest, Query } from "@ixeta/xams";
import { useQuery } from "@tanstack/react-query";
import { Table, Loader } from "@mantine/core";

type Widget = {
  WidgetId: string;
  Name: string;
  Price: number;
};

function WidgetList() {
  const authRequest = useAuthRequest();

  const { data: widgets, isLoading, error } = useQuery({
    queryKey: ["widgets"],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Widget")
        .where("Active", "==", "true")
        .orderBy("Name", "asc")
        .toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results;
    },
  });

  if (isLoading) return <Loader />;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <Table>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Name</Table.Th>
          <Table.Th>Price</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {widgets?.map((widget) => (
          <Table.Tr key={widget.WidgetId}>
            <Table.Td>{widget.Name}</Table.Td>
            <Table.Td>${widget.Price.toFixed(2)}</Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}
```

---

### Pattern 2: Create/Edit Form

```tsx
import { useAuthRequest } from "@ixeta/xams";
import { TextInput, NumberInput, Button, Stack } from "@mantine/core";
import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";

function WidgetForm({ widgetId }: { widgetId?: string }) {
  const authRequest = useAuthRequest();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [price, setPrice] = useState(0);
  const [loading, setLoading] = useState(false);

  // Load existing widget if editing
  const { data: widget } = useQuery({
    queryKey: ["widget", widgetId],
    queryFn: async () => {
      if (!widgetId) return null;
      const query = new Query(["*"])
        .from("Widget")
        .where("WidgetId", "==", widgetId)
        .toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results[0];
    },
    enabled: !!widgetId,
  });

  // Populate form when widget loads
  useEffect(() => {
    if (widget) {
      setName(widget.Name);
      setPrice(widget.Price);
    }
  }, [widget]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    const data = { Name: name, Price: price };
    const response = widgetId
      ? await authRequest.update("Widget", { WidgetId: widgetId, ...data })
      : await authRequest.create("Widget", data);

    if (response.succeeded) {
      queryClient.invalidateQueries({ queryKey: ["widgets"] });
      console.log("Saved successfully");
    } else {
      console.error(response.friendlyMessage);
    }
    setLoading(false);
  };

  return (
    <form onSubmit={handleSubmit}>
      <Stack>
        <TextInput
          label="Name"
          value={name}
          onChange={(e) => setName(e.currentTarget.value)}
          required
        />
        <NumberInput
          label="Price"
          value={price}
          onChange={(val) => setPrice(val as number)}
          min={0}
          decimalScale={2}
        />
        <Button type="submit" loading={loading}>
          {widgetId ? "Update" : "Create"}
        </Button>
      </Stack>
    </form>
  );
}
```

---

### Pattern 3: Master-Detail View

```tsx
function OrderWithLineItems({ orderId }: { orderId: string }) {
  const authRequest = useAuthRequest();

  // Load order with joined line items
  const { data: order } = useQuery({
    queryKey: ["order", orderId],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Order")
        .join("Order.OrderId", "LineItem.OrderId", "li", ["*"])
        .where("OrderId", "==", orderId)
        .denormalize()
        .toReadRequest();
      const response = await authRequest.read(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results[0];
    },
  });

  return (
    <div>
      <h2>Order #{order?.OrderNumber}</h2>
      <p>Total: ${order?.Total}</p>

      <h3>Line Items</h3>
      <Table>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Product</Table.Th>
            <Table.Th>Quantity</Table.Th>
            <Table.Th>Price</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {order?.li?.map((item: any) => (
            <Table.Tr key={item.LineItemId}>
              <Table.Td>{item.ProductName}</Table.Td>
              <Table.Td>{item.Quantity}</Table.Td>
              <Table.Td>${item.Price}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </div>
  );
}
```

---

### Pattern 4: Search with Debounce

```tsx
import { useAuthRequest, Query } from "@ixeta/xams";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useDebouncedState } from "@mantine/hooks";
import { TextInput } from "@mantine/core";
import { useRef } from "react";

function WidgetSearch() {
  const authRequest = useAuthRequest();
  const queryClient = useQueryClient();
  const [search, setSearch] = useDebouncedState("", 300);
  const searchRef = useRef(search);

  const { data: widgets } = useQuery({
    queryKey: ["widgets"],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Widget")
        .where("Name", "Contains", searchRef.current)
        .orderBy("Name", "asc")
        .top(100)
        .toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results;
    },
  });

  // Invalidate query when search changes
  if (searchRef.current !== search) {
    searchRef.current = search;
    queryClient.invalidateQueries({ queryKey: ["widgets"] });
  }

  return (
    <div>
      <TextInput
        placeholder="Search widgets..."
        defaultValue={search}
        onChange={(e) => setSearch(e.currentTarget.value)}
      />
      <div>
        {widgets?.map((widget) => (
          <div key={widget.WidgetId}>{widget.Name}</div>
        ))}
      </div>
    </div>
  );
}
```

---

### Pattern 5: Bulk Operations

```tsx
function BulkOperations() {
  const authRequest = useAuthRequest();
  const queryClient = useQueryClient();

  const processBulk = async (selectedIds: string[]) => {
    const updates = selectedIds.map((id) => ({
      WidgetId: id,
      Status: "Processed",
      ProcessedDate: new Date().toISOString(),
    }));

    const response = await authRequest.bulkUpdate(updates);

    if (response.succeeded) {
      queryClient.invalidateQueries({ queryKey: ["widgets"] });
      console.log("Bulk update successful");
    } else {
      console.error(response.friendlyMessage);
    }
  };

  return <Button onClick={() => processBulk(selectedIds)}>Process Selected</Button>;
}
```

---

### Pattern 6: Conditional UI Based on Permissions

```tsx
function WidgetActions({ widget }: { widget: Widget }) {
  const authRequest = useAuthRequest();
  const [canEdit, setCanEdit] = useState(false);
  const [canDelete, setCanDelete] = useState(false);

  useEffect(() => {
    authRequest
      .hasAnyPermissions([
        "TABLE_Widget_UPDATE_USER",
        "TABLE_Widget_UPDATE_TEAM",
        "TABLE_Widget_UPDATE_SYSTEM",
      ])
      .then(setCanEdit);

    authRequest
      .hasAnyPermissions([
        "TABLE_Widget_DELETE_USER",
        "TABLE_Widget_DELETE_TEAM",
        "TABLE_Widget_DELETE_SYSTEM",
      ])
      .then(setCanDelete);
  }, []);

  return (
    <Group>
      {canEdit && <Button onClick={() => handleEdit(widget)}>Edit</Button>}
      {canDelete && <Button color="red" onClick={() => handleDelete(widget)}>Delete</Button>}
    </Group>
  );
}
```

---

### Pattern 7: Delete with Confirmation

```tsx
function DeleteWidget({ widget }: { widget: Widget }) {
  const authRequest = useAuthRequest();
  const appContext = useAppContext();
  const queryClient = useQueryClient();

  const handleDelete = () => {
    appContext.showConfirm(
      `Are you sure you want to delete "${widget.Name}"?`,
      async () => {
        const response = await authRequest.delete("Widget", widget.WidgetId);
        if (response.succeeded) {
          queryClient.invalidateQueries({ queryKey: ["widgets"] });
        } else {
          appContext.showError(response.friendlyMessage, "Delete Failed");
        }
      },
      () => console.log("Delete cancelled"),
      "Confirm Delete"
    );
  };

  return <Button color="red" onClick={handleDelete}>Delete</Button>;
}
```

---

### Pattern 8: File Upload

```tsx
function FileUpload() {
  const authRequest = useAuthRequest();
  const appContext = useAppContext();
  const [file, setFile] = useState<File | null>(null);

  const handleUpload = async () => {
    if (!file) return;

    appContext.showLoading("Uploading...");

    const formData = new FormData();
    formData.append("file", file);
    formData.append("name", "ImportWidgets");
    formData.append("parameters", JSON.stringify({ category: "products" }));

    const response = await authRequest.file(formData);

    appContext.hideLoading();

    if (response.succeeded) {
      console.log("Upload successful");
    } else {
      appContext.showError(response.friendlyMessage, "Upload Failed");
    }
  };

  return (
    <div>
      <FileInput
        placeholder="Select file"
        value={file}
        onChange={setFile}
      />
      <Button onClick={handleUpload} disabled={!file}>
        Upload
      </Button>
    </div>
  );
}
```

---

### Pattern 9: Lookup Select (Options/References)

```tsx
import { Select } from "@mantine/core";

function WidgetFormWithCategory() {
  const authRequest = useAuthRequest();
  const [categoryId, setCategoryId] = useState<string | null>(null);

  // Load category options
  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => {
      const query = new Query(["OptionId", "Label", "Value"])
        .from("Option")
        .where("Name", "==", "WidgetCategory")
        .orderBy("Order", "asc")
        .toReadRequest();
      const response = await authRequest.read(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results;
    },
  });

  const handleSubmit = async () => {
    await authRequest.create("Widget", {
      Name: name,
      CategoryId: categoryId,
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      <Select
        label="Category"
        placeholder="Select category"
        value={categoryId}
        onChange={setCategoryId}
        data={categories?.map((cat) => ({
          value: cat.OptionId,
          label: cat.Label,
        }))}
      />
      <Button type="submit">Create</Button>
    </form>
  );
}
```

---

### Pattern 10: Pagination

```tsx
function PaginatedList() {
  const authRequest = useAuthRequest();
  const [page, setPage] = useState(1);
  const pageSize = 25;

  const { data } = useQuery({
    queryKey: ["widgets", page],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Widget")
        .orderBy("CreatedDate", "desc")
        .top(pageSize)
        .page(page)
        .toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data;
    },
  });

  return (
    <div>
      <Table>
        {/* Table rows */}
      </Table>
      <Pagination
        total={data?.pages ?? 1}
        value={page}
        onChange={setPage}
      />
    </div>
  );
}
```

---

## Best Practices

1. **Always wrap your app with AuthContextProvider** - Required for API calls
2. **Use AppContextProvider for user feedback** - Shows errors, loading states, confirmations
3. **Use TypeScript generics** - `authRequest.read<Widget>(request)` for type safety
4. **Handle permissions** - Check permissions before showing UI elements
5. **Optimize queries** - Use `fields` parameter to fetch only needed data
6. **Implement error handling** - Always check `response.succeeded` before using data
7. **Use bulk operations for multiple records** - More efficient than individual calls
8. **Use Tanstack Query** - For caching, refetching, and optimistic updates
9. **Leverage metadata** - `authRequest.metadata()` provides field information
10. **Use Query builder** - More readable than manual ReadRequest objects

---

## API Endpoints

The following endpoints are used internally by useAuthRequest:

```tsx
export const API_DATA_PERMISSIONS = "/xams/permissions";
export const API_DATA_ACTION = "/xams/action";
export const API_DATA_CREATE = "/xams/create";
export const API_DATA_READ = "/xams/read";
export const API_DATA_UPDATE = "/xams/update";
export const API_DATA_DELETE = "/xams/delete";
export const API_DATA_FILE = "/xams/file";
export const API_DATA_METADATA = "/xams/metadata";
export const API_CONFIG = "/xams/config";
```

---

## Type Exports

All TypeScript types are exported and available for use:

```tsx
import type {
  // Hooks
  useAuthRequestType,

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
  MetadataField,
  BulkRequest,

  // Admin
  AdminDashboardProps,
  NavItem,

  // Context
  AuthContextProviderProps,
  AppContextShape,
  AuthContextShape,
} from "@ixeta/xams";
```

---

## Support

For issues, questions, or contributions:

- **Documentation**: https://xams.io
- **GitHub**: https://github.com/ixeta/xams
- **Email**: support@ixeta.net

---

**Last Updated**: 2025-10-14
**Version**: 1.0.16
