# Xams Lookup & Selection Components Reference

← [Back to Main Documentation](../CLAUDE.md) | [Data Components](components-data.md)

## Table of Contents

1. [Lookup](#lookup)
2. [RichText](#richtext)
3. [ToggleMode](#togglemode)

---

## Lookup

A searchable dropdown component for selecting related records from foreign key relationships.

### Import
```tsx
import { Lookup } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `label` | `React.ReactNode` | ❌ | Field label |
| `metaDataField` | `MetadataField` | ✅ | Field metadata from entity |
| `owningTableName` | `string` | ❌ | Parent table name |
| `defaultLabelValue` | `DataItem` | ❌ | Initial display value |
| `value` | `string` | ❌ | Selected value (ID) |
| `onChange` | `(value: LookupValue \| null) => void` | ❌ | Change handler |
| `onBlur` | `() => void` | ❌ | Blur handler |
| `excludeValues` | `string[]` | ❌ | IDs to exclude from options |
| `query` | `LookupQuery` | ❌ | Custom query filters |
| `readOnly` | `boolean` | ❌ | Make field read-only |
| `required` | `boolean` | ❌ | Field is required |
| `error` | `string` | ❌ | Error message to display |
| `className` | `string` | ❌ | CSS class names |
| `disabled` | `boolean` | ❌ | Disable the field |
| `size` | `string` | ❌ | Field size |

### Type Definitions

```tsx
interface DataItem {
  label: string;      // Display text
  value: string;      // Record ID
  description?: string; // Secondary text
  data?: string;      // Additional data
}

interface LookupValue {
  id: string | null;    // Selected record ID
  value: string | null; // Selected record value
  label: string | null; // Display text
}

interface LookupQuery {
  field: string;
  filters: ReadFilter[];
  orderBy?: ReadOrderBy[];
}

interface MetadataField {
  name: string;
  lookupTableName: string;
  lookupTablePrimaryKeyField: string;
  lookupTableNameField: string;
  lookupTableDescriptionField?: string;
  option: string; // For Option lookups
}
```

### Usage Examples

#### Basic Lookup (Used Automatically by Field Component)
```tsx
// This is typically handled automatically by the Field component
// when it detects a lookup field type
<Field name="CategoryId" />
```

#### Standalone Lookup
```tsx
<Lookup
  metaDataField={categoryField}
  value={selectedCategoryId}
  onChange={(value) => {
    console.log("Selected:", value?.id, value?.label);
    setSelectedCategoryId(value?.id);
  }}
  required={true}
  label="Product Category"
/>
```

#### With Exclusions
```tsx
<Lookup
  metaDataField={userField}
  excludeValues={["user-1", "user-2"]} // Exclude these users
  onChange={handleUserChange}
/>
```

#### With Custom Query
```tsx
<Lookup
  metaDataField={statusField}
  query={{
    field: "StatusId",
    filters: [
      { field: "IsActive", operator: "=", value: true },
      { field: "Type", operator: "=", value: "Widget" }
    ],
    orderBy: [
      { field: "Order", direction: "asc" }
    ]
  }}
  onChange={handleStatusChange}
/>
```

### Features

- **Searchable**: Type to search through available options
- **Debounced Search**: Reduces API calls while typing
- **Description Support**: Shows secondary text for each option
- **Option Support**: Works with the Option entity system
- **Lazy Loading**: Loads data on dropdown open
- **Validation**: Integrates with form validation
- **Accessibility**: Full keyboard navigation support

### Integration with Field Component

The Field component automatically uses Lookup for foreign key fields:

```tsx
// In your entity
public class Widget : BaseEntity {
    [UIOption("Category")]
    public Guid? CategoryId { get; set; }
    public Option? Category { get; set; }
    
    public Guid? UserId { get; set; }
    public User? User { get; set; }
}

// In your form - Field automatically renders as Lookup
<Field name="CategoryId" />  // Renders Option lookup
<Field name="UserId" />      // Renders User lookup
```

---

## RichText

A rich text editor component with formatting capabilities.

### Import
```tsx
import { RichText } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `value` | `string` | ❌ | HTML content |
| `onChange` | `(value: string) => void` | ❌ | Change handler |
| `placeholder` | `string` | ❌ | Placeholder text |
| `readOnly` | `boolean` | ❌ | Make editor read-only |
| `minHeight` | `number` | ❌ | Minimum editor height |
| `maxHeight` | `number` | ❌ | Maximum editor height |

### Usage Examples

#### Basic Rich Text Editor
```tsx
<RichText
  value={content}
  onChange={setContent}
  placeholder="Enter description..."
/>
```

#### With Field Component
```tsx
// Automatically renders as RichText when variant is specified
<Field name="Description" varient="rich" />
```

#### Read-Only Display
```tsx
<RichText
  value={htmlContent}
  readOnly={true}
/>
```

### Features

- **Formatting Options**: Bold, italic, underline, lists, etc.
- **HTML Support**: Stores and displays HTML content
- **Toolbar**: Customizable formatting toolbar
- **Responsive**: Adapts to container size

---

## ToggleMode

A component for toggling between different display modes (typically light/dark theme).

### Import
```tsx
import { ToggleMode } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `size` | `"xs" \| "sm" \| "md" \| "lg" \| "xl"` | ❌ | Toggle size |
| `variant` | `"default" \| "filled" \| "outline"` | ❌ | Visual style |
| `onChange` | `(mode: "light" \| "dark") => void` | ❌ | Mode change handler |

### Usage Example

```tsx
<ToggleMode 
  size="md"
  onChange={(mode) => {
    console.log("Theme changed to:", mode);
  }}
/>
```

---

## Complete Example: Custom Form with Lookups

```tsx
import { 
  FormContainer, 
  Field, 
  Lookup, 
  RichText,
  SaveButton,
  useFormBuilder 
} from "@ixeta/xams";
import { Grid } from "@mantine/core";

function ProductForm({ productId }) {
  const formBuilder = useFormBuilder({
    tableName: "Product",
    id: productId,
    lookupExclusions: [{
      fieldName: "CategoryId",
      values: ["archived-category"] // Exclude archived categories
    }],
    lookupQueries: [{
      field: "SupplierId",
      filters: [
        { field: "IsActive", operator: "=", value: true }
      ]
    }]
  });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Grid>
        <Grid.Col span={12}>
          <Field name="Name" required />
        </Grid.Col>
        
        <Grid.Col span={6}>
          {/* Automatically renders as Lookup */}
          <Field name="CategoryId" />
        </Grid.Col>
        
        <Grid.Col span={6}>
          {/* Automatically renders as Lookup with query */}
          <Field name="SupplierId" />
        </Grid.Col>
        
        <Grid.Col span={12}>
          {/* Rich text editor for description */}
          <Field name="Description" varient="rich" />
        </Grid.Col>
        
        <Grid.Col span={6}>
          <Field name="Price" />
        </Grid.Col>
        
        <Grid.Col span={6}>
          {/* Option lookup for status */}
          <Field name="StatusId" />
        </Grid.Col>
        
        <Grid.Col span={12}>
          <SaveButton />
        </Grid.Col>
      </Grid>
    </FormContainer>
  );
}
```

## Advanced Lookup Patterns

### Dynamic Lookup Filtering

```tsx
function LinkedLookups() {
  const [countryId, setCountryId] = useState<string | null>(null);
  const formBuilder = useFormBuilder({
    tableName: "Address",
    lookupQueries: countryId ? [{
      field: "CityId",
      filters: [
        { field: "CountryId", operator: "=", value: countryId }
      ]
    }] : []
  });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Field 
        name="CountryId" 
        onChange={(value) => {
          setCountryId(value);
          // Clear city when country changes
          formBuilder.setField("CityId", null);
        }}
      />
      <Field name="CityId" />
    </FormContainer>
  );
}
```

### Custom Lookup Rendering

```tsx
function CustomLookupForm() {
  const formBuilder = useFormBuilder({ tableName: "Order" });
  
  // Access the metadata field for custom rendering
  const customerField = formBuilder.metadata?.fields.find(
    f => f.name === "CustomerId"
  );

  if (!customerField) return null;

  return (
    <FormContainer formBuilder={formBuilder}>
      <Lookup
        metaDataField={customerField}
        value={formBuilder.data.CustomerId}
        onChange={(value) => {
          formBuilder.setField("CustomerId", value?.id);
          // Additional logic when customer changes
          if (value?.id) {
            loadCustomerDefaults(value.id);
          }
        }}
        label={
          <div className="flex items-center gap-2">
            <span>Customer</span>
            <Badge>Required</Badge>
          </div>
        }
        required={true}
      />
    </FormContainer>
  );
}
```

---

← [Data Components](components-data.md) | [Main Documentation](../CLAUDE.md) →