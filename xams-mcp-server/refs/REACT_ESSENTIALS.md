# @ixeta/xams - React Essentials

**Quick Reference** | Version 1.0.16 | [Full API →](REACT_API_REFERENCE.md)

Essential hooks, utilities, and patterns for building Xams applications with React.

---

## Setup

```tsx
import { AuthContextProvider, AppContextProvider } from "@ixeta/xams";
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";

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

---

## useAuthRequest

Primary hook for authenticated API calls. All CRUD operations use this hook.

```tsx
const authRequest = useAuthRequest();

// Create
const response = await authRequest.create("Widget", {
  Name: "New Widget",
  Price: 19.99,
  Active: true,
});

// Read with Query builder
import { Query } from "@ixeta/xams";
const readRequest = new Query(["WidgetId", "Name", "Price"])
  .from("Widget")
  .where("Active", "==", "true")
  .orderBy("Name", "asc")
  .top(50)
  .toReadRequest();
const response = await authRequest.read<Widget>(readRequest);

// Read with ReadRequest object
const response = await authRequest.read({
  tableName: "Widget",
  fields: ["WidgetId", "Name", "Price"],
  filters: [{ field: "Active", operator: "==", value: "true" }],
  orderBy: [{ field: "Name", order: "asc" }],
  maxResults: 50,
});

// Update
await authRequest.update("Widget", {
  WidgetId: "123e4567-e89b-12d3-a456-426614174000",
  Price: 24.99,
});

// Delete
await authRequest.delete("Widget", widgetId);

// Upsert (create or update)
await authRequest.upsert("Widget", {
  WidgetId: widgetId, // If exists update, else create
  Name: "Updated Widget",
});

// Custom Action
const response = await authRequest.action("MyAction", {
  param1: "value1",
  param2: 5,
});

// Bulk Operations
await authRequest.bulkCreate([
  { Name: "Widget 1", Price: 10 },
  { Name: "Widget 2", Price: 20 },
]);
await authRequest.bulkUpdate([...]);
await authRequest.bulkDelete([...]);

// Permissions
const canEdit = await authRequest.hasAllPermissions(["TABLE_Widget_UPDATE_SYSTEM"]);
const canView = await authRequest.hasAnyPermissions([
  "TABLE_Widget_READ_USER",
  "TABLE_Widget_READ_TEAM",
  "TABLE_Widget_READ_SYSTEM",
]);

// Metadata
const metadata = await authRequest.metadata("Widget");

// Tables
const tables = await authRequest.tables(); // All tables
const appTables = await authRequest.tables("app"); // Tagged tables

// Current User
const user = await authRequest.whoAmI();

// File Upload
const formData = new FormData();
formData.append("file", fileInput.files[0]);
formData.append("name", "MyAction");
formData.append("parameters", JSON.stringify({ param: "value" }));
await authRequest.file(formData);
```

**All Methods**: create, read, update, delete, upsert, bulkCreate, bulkUpdate, bulkDelete, bulk, action, file, metadata, tables, whoAmI, hasAllPermissions, hasAnyPermissions, config, execute

[Full API →](REACT_API_REFERENCE.md#useauthrequest)

---

## Query Builder

Build complex queries with fluent API.

```tsx
import { Query } from "@ixeta/xams";

// Basic query
const query = new Query(["*"])
  .from("Widget")
  .toReadRequest();

// With filters
const query = new Query(["Name", "Price"])
  .from("Widget")
  .where("Active", "==", "true")
  .and("Price", ">", "10")
  .toReadRequest();

// With joins
const query = new Query(["*"])
  .from("Account")
  .join("Account.AccountId", "Contact.AccountId", "c", ["FirstName", "LastName"])
  .where("c.FirstName", "Contains", "John")
  .toReadRequest();

// With ordering and paging
const query = new Query(["*"])
  .from("Widget")
  .orderBy("Name", "asc")
  .top(25)
  .page(2)
  .toReadRequest();

// Denormalized (joins as arrays)
const query = new Query(["AccountId", "Name"])
  .from("Account")
  .join("Account.AccountId", "Contact.AccountId", "c", ["*"])
  .denormalize()
  .toReadRequest();

// Use with authRequest
const response = await authRequest.read<Widget>(query);
```

---

## useAppContext

Application-level utilities for UI feedback.

```tsx
const appContext = useAppContext();

// Show error modal
appContext.showError("Something went wrong", "Error Title");

// Show loading overlay
appContext.showLoading("Processing...");
appContext.hideLoading();

// Show confirmation dialog
appContext.showConfirm(
  "Are you sure?",
  () => console.log("Confirmed"),
  () => console.log("Cancelled"),
  "Confirm Action"
);

// SignalR connection
const signalR = await appContext.signalR();
const connectionState = appContext.signalRState; // HubConnectionState

// Current user ID
const userId = appContext.userId;
```

---

## Context Providers

### AuthContextProvider
Required for all API calls. Provides authentication and request context.

```tsx
<AuthContextProvider
  apiUrl="https://api.example.com"
  headers={{ UserId: userId }}
  onUnauthorized={() => router.push("/login")}
  getAccessToken={async () => await getToken()}
>
  {children}
</AuthContextProvider>
```

**Props**: apiUrl (required), headers, onUnauthorized, withCredentials, getAccessToken

**Hook**: `useAuthContext()` - Access auth config

### AppContextProvider
Provides UI utilities (showError, showLoading, showConfirm, SignalR).

```tsx
<AppContextProvider>
  {children}
</AppContextProvider>
```

**Hook**: `useAppContext()` - Access app utilities

---

## Core Types

### ApiResponse<T>
```tsx
interface ApiResponse<T> {
  succeeded: boolean;
  data: T;
  friendlyMessage: string;
  logMessage: string;
}

// Always check succeeded before using data
if (response.succeeded) {
  const widget = response.data;
}
```

### ReadRequest
```tsx
interface ReadRequest {
  tableName: string;
  fields?: string[];
  filters?: ReadFilter[];
  orderBy?: ReadOrderBy[];
  maxResults?: number;
  page?: number;
  joins?: ReadJoin[];
  except?: ReadExcept[];
  distinct?: boolean;
  denormalize?: boolean;
  parameters?: any;
}
```

### ReadFilter
```tsx
interface ReadFilter {
  field?: string;
  operator?: "==" | "!=" | ">" | ">=" | "<" | "<=" | "contains";
  value?: string | null;
  logicalOperator?: "AND" | "OR";
  filters?: ReadFilter[]; // For nested filters
}

// Examples
{ field: "Active", operator: "==", value: "true" }
{ field: "Price", operator: ">=", value: "10" }
{
  logicalOperator: "OR",
  filters: [
    { field: "Status", operator: "==", value: "Active" },
    { field: "Status", operator: "==", value: "Pending" }
  ]
}
```

### ReadResponse<T>
```tsx
interface ReadResponse<T> {
  results: T[];
  totalResults: number;
  pages: number;
  currentPage: number;
  maxResults: number;
  tableName: string;
  orderBy?: ReadOrderBy[];
}
```

### ReadOrderBy
```tsx
interface ReadOrderBy {
  field: string;
  order?: "asc" | "desc";
}
```

### ReadJoin
```tsx
interface ReadJoin {
  fromTable: string;
  fromField: string;
  toTable: string;
  toField: string;
  alias?: string;
  fields: string[];
  filters?: ReadFilter[];
}
```

---

## Common Patterns

### Pattern 1: Display List with Mantine

```tsx
import { useAuthRequest, Query } from "@ixeta/xams";
import { useQuery } from "@tanstack/react-query";
import { Table } from "@mantine/core";

function WidgetList() {
  const authRequest = useAuthRequest();

  const { data, isLoading } = useQuery({
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

  if (isLoading) return <div>Loading...</div>;

  return (
    <Table>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Name</Table.Th>
          <Table.Th>Price</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {data?.map((widget) => (
          <Table.Tr key={widget.WidgetId}>
            <Table.Td>{widget.Name}</Table.Td>
            <Table.Td>${widget.Price}</Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}
```

### Pattern 2: Create/Edit Form with Mantine

```tsx
import { useAuthRequest } from "@ixeta/xams";
import { TextInput, NumberInput, Button } from "@mantine/core";
import { useState } from "react";

function WidgetForm({ widgetId }: { widgetId?: string }) {
  const authRequest = useAuthRequest();
  const [name, setName] = useState("");
  const [price, setPrice] = useState(0);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);

    const data = { Name: name, Price: price };
    const response = widgetId
      ? await authRequest.update("Widget", { WidgetId: widgetId, ...data })
      : await authRequest.create("Widget", data);

    if (response.succeeded) {
      console.log("Saved successfully");
    } else {
      console.error(response.friendlyMessage);
    }
    setLoading(false);
  };

  return (
    <form onSubmit={handleSubmit}>
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
      />
      <Button type="submit" loading={loading}>
        Save
      </Button>
    </form>
  );
}
```

### Pattern 3: Load Record by ID

```tsx
import { useAuthRequest, Query } from "@ixeta/xams";
import { useQuery } from "@tanstack/react-query";

function WidgetDetail({ widgetId }: { widgetId: string }) {
  const authRequest = useAuthRequest();

  const { data: widget } = useQuery({
    queryKey: ["widget", widgetId],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Widget")
        .where("WidgetId", "==", widgetId)
        .toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results[0];
    },
  });

  return <div>{widget?.Name}</div>;
}
```

### Pattern 4: Master-Detail with Related Records

```tsx
function OrderWithLineItems({ orderId }: { orderId: string }) {
  const authRequest = useAuthRequest();

  // Load order with line items joined
  const { data } = useQuery({
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
      <h2>Order: {data?.OrderNumber}</h2>
      <h3>Line Items</h3>
      <ul>
        {data?.li?.map((item) => (
          <li key={item.LineItemId}>
            {item.ProductName} - ${item.Price}
          </li>
        ))}
      </ul>
    </div>
  );
}
```

### Pattern 5: Filtered Search

```tsx
import { useDebouncedState } from "@mantine/hooks";
import { TextInput } from "@mantine/core";

function WidgetSearch() {
  const authRequest = useAuthRequest();
  const queryClient = useQueryClient();
  const [search, setSearch] = useDebouncedState("", 300);
  const searchRef = useRef(search);

  const { data } = useQuery({
    queryKey: ["widgets"],
    queryFn: async () => {
      const query = new Query(["*"])
        .from("Widget")
        .where("Name", "Contains", searchRef.current)
        .orderBy("Name", "asc")
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
      {data?.map((widget) => (
        <div key={widget.WidgetId}>{widget.Name}</div>
      ))}
    </div>
  );
}
```

### Pattern 6: Delete with Confirmation

```tsx
function WidgetActions({ widget }: { widget: Widget }) {
  const authRequest = useAuthRequest();
  const appContext = useAppContext();
  const queryClient = useQueryClient();

  const handleDelete = () => {
    appContext.showConfirm(
      `Delete ${widget.Name}?`,
      async () => {
        const response = await authRequest.delete("Widget", widget.WidgetId);
        if (response.succeeded) {
          queryClient.invalidateQueries({ queryKey: ["widgets"] });
        } else {
          appContext.showError(response.friendlyMessage);
        }
      },
      () => console.log("Cancelled"),
      "Confirm Delete"
    );
  };

  return <Button onClick={handleDelete}>Delete</Button>;
}
```

### Pattern 7: Bulk Operations

```tsx
function BulkUpdate({ selectedIds }: { selectedIds: string[] }) {
  const authRequest = useAuthRequest();

  const handleBulkUpdate = async () => {
    const updates = selectedIds.map((id) => ({
      WidgetId: id,
      Status: "Processed",
    }));

    const response = await authRequest.bulkUpdate(updates);

    if (response.succeeded) {
      console.log("Bulk update successful");
    }
  };

  return <Button onClick={handleBulkUpdate}>Mark as Processed</Button>;
}
```

### Pattern 8: Conditional Rendering Based on Permissions

```tsx
function WidgetActions({ widget }: { widget: Widget }) {
  const authRequest = useAuthRequest();
  const [canEdit, setCanEdit] = useState(false);

  useEffect(() => {
    authRequest
      .hasAnyPermissions([
        "TABLE_Widget_UPDATE_USER",
        "TABLE_Widget_UPDATE_TEAM",
        "TABLE_Widget_UPDATE_SYSTEM",
      ])
      .then(setCanEdit);
  }, []);

  return (
    <div>
      {canEdit && <Button>Edit</Button>}
      {canEdit && <Button>Delete</Button>}
    </div>
  );
}
```

---

## Utilities

### Query Class

Fluent API for building ReadRequest objects.

```tsx
import { Query } from "@ixeta/xams";

new Query(fields: string[])
  .from(tableName: string)
  .where(field: string, operator: string, value: any)
  .and(field: string, operator: string, value: any)
  .or(field: string, operator: string, value: any)
  .join(fromField: string, toField: string, alias: string, fields: string[])
  .leftJoin(fromField: string, toField: string, alias: string, fields: string[])
  .orderBy(field: string, order: "asc" | "desc")
  .top(count: number)
  .page(pageNumber: number)
  .distinct()
  .denormalize()
  .toReadRequest() // Returns ReadRequest
```

### URL Helpers

```tsx
import { getQueryParam, getUserIdUrlPart } from "@ixeta/xams";

// Get query parameter from URL
const userId = getQueryParam("userid", router.asPath);

// Add userId to navigation URL
import { getUserIdUrlPart } from "@ixeta/xams";
router.push(getUserIdUrlPart(router.asPath, "/project"));
```

---

## AdminDashboard

Full-featured admin interface (typically used at `/admin` route).

```tsx
import { AdminDashboard } from "@ixeta/xams";
import { NavLink } from "@mantine/core";
import { IconLogout } from "@tabler/icons-react";

function Admin() {
  return (
    <AdminDashboard
      addMenuItems={[
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

**Props**: addMenuItems, visibleEntities, hiddenEntities, title, etc.

---

## Best Practices

1. **Always use Tanstack Query** for server state management
2. **Check response.succeeded** before using data
3. **Use TypeScript generics** - `authRequest.read<Widget>(request)`
4. **Leverage metadata** - `authRequest.metadata("Widget")` for field info
5. **Select only needed fields** - Don't use `["*"]` unless necessary
6. **Use Query builder** - Cleaner than manual ReadRequest objects
7. **Handle errors gracefully** - Use appContext.showError()
8. **Invalidate queries** - Use queryClient.invalidateQueries after mutations
9. **Check permissions first** - Use hasAllPermissions/hasAnyPermissions
10. **Avoid unnecessary useEffect** - [See guide](../you-might-not-need-an-effect.md)

---

## Complete Example: CRUD with Mantine

```tsx
import { useAuthRequest, Query } from "@ixeta/xams";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Table, Button, Modal, TextInput, NumberInput } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import { useState } from "react";

type Widget = {
  WidgetId: string;
  Name: string;
  Price: number;
};

function WidgetManager() {
  const authRequest = useAuthRequest();
  const queryClient = useQueryClient();
  const [opened, { open, close }] = useDisclosure(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [price, setPrice] = useState(0);

  // Query widgets
  const { data: widgets } = useQuery({
    queryKey: ["widgets"],
    queryFn: async () => {
      const query = new Query(["*"]).from("Widget").orderBy("Name", "asc").toReadRequest();
      const response = await authRequest.read<Widget>(query);
      if (!response.succeeded) throw new Error(response.friendlyMessage);
      return response.data.results;
    },
  });

  // Handle create/update
  const handleSave = async () => {
    const data = { Name: name, Price: price };
    const response = editingId
      ? await authRequest.update("Widget", { WidgetId: editingId, ...data })
      : await authRequest.create("Widget", data);

    if (response.succeeded) {
      queryClient.invalidateQueries({ queryKey: ["widgets"] });
      close();
      setName("");
      setPrice(0);
      setEditingId(null);
    }
  };

  // Handle delete
  const handleDelete = async (widget: Widget) => {
    const response = await authRequest.delete("Widget", widget.WidgetId);
    if (response.succeeded) {
      queryClient.invalidateQueries({ queryKey: ["widgets"] });
    }
  };

  // Open form for edit
  const handleEdit = (widget: Widget) => {
    setEditingId(widget.WidgetId);
    setName(widget.Name);
    setPrice(widget.Price);
    open();
  };

  return (
    <div>
      <Button onClick={open}>Create Widget</Button>

      <Table>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Price</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {widgets?.map((widget) => (
            <Table.Tr key={widget.WidgetId}>
              <Table.Td>{widget.Name}</Table.Td>
              <Table.Td>${widget.Price}</Table.Td>
              <Table.Td>
                <Button size="xs" onClick={() => handleEdit(widget)}>
                  Edit
                </Button>
                <Button size="xs" color="red" onClick={() => handleDelete(widget)}>
                  Delete
                </Button>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Modal opened={opened} onClose={close} title={editingId ? "Edit Widget" : "Create Widget"}>
        <TextInput
          label="Name"
          value={name}
          onChange={(e) => setName(e.currentTarget.value)}
          required
        />
        <NumberInput label="Price" value={price} onChange={(val) => setPrice(val as number)} min={0} />
        <Button onClick={handleSave}>Save</Button>
      </Modal>
    </div>
  );
}
```

---

## Need More?

- **Full useAuthRequest API**: [REACT_API_REFERENCE.md#useauthrequest](REACT_API_REFERENCE.md#useauthrequest)
- **All Type Definitions**: [REACT_API_REFERENCE.md#api-types](REACT_API_REFERENCE.md#api-types)
- **AdminDashboard Props**: [REACT_API_REFERENCE.md#admindashboard](REACT_API_REFERENCE.md#admindashboard)
