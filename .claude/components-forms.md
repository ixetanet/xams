# Xams Form Components Reference

← [Back to Main Documentation](../CLAUDE.md) | [Component Index](components.md)

## Table of Contents

1. [FormContainer](#formcontainer)
2. [Field](#field)
3. [SaveButton](#savebutton)
4. [useFormBuilder Hook](#useformbuilder-hook)

---

## FormContainer

The main wrapper component for forms that provides context and handles form submission.

### Import
```tsx
import { FormContainer } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `formBuilder` | `useFormBuilderType` | ✅ | FormBuilder instance from useFormBuilder hook |
| `className` | `string` | ❌ | CSS class names to apply to the form element |
| `children` | `React.ReactNode` | ❌ | Form content (Field components, layout, etc.) |
| `showLoading` | `boolean` | ❌ | Force display loading state |
| `onPreValidate` | `PreSaveEvent` | ❌ | Hook called before validation runs |
| `onPreSave` | `PreSaveEvent` | ❌ | Hook called before save (can cancel by returning false) |
| `onPostSave` | `PostSaveEvent` | ❌ | Hook called after successful save |

### Type Definitions

```tsx
type PreSaveEvent = (
  operation: "CREATE" | "UPDATE",
  submissionData: any,
  parameters?: any
) => boolean | void;

type PostSaveEvent = (
  operation: "CREATE" | "UPDATE", 
  id: string,
  data: any
) => void;
```

### Usage Example

```tsx
const formBuilder = useFormBuilder({ tableName: "Widget" });

<FormContainer 
  formBuilder={formBuilder}
  onPreSave={(operation, data) => {
    console.log("Saving:", operation, data);
    return true; // Continue with save
  }}
  onPostSave={(operation, id) => {
    console.log("Saved:", operation, id);
  }}
>
  <Field name="Name" />
  <Field name="Price" />
  <SaveButton />
</FormContainer>
```

### Features
- Automatically handles form submission
- Shows loading state during async operations
- Displays permission errors when user lacks access
- Manages form validation state
- Provides FormContext to child components

---

## Field

Renders a form field based on entity metadata. Automatically selects the appropriate input type.

### Import
```tsx
import { Field } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | ✅ | Field name from entity metadata |
| `label` | `string \| React.ReactNode` | ❌ | Override field label |
| `focus` | `boolean` | ❌ | Auto-focus this field on mount |
| `varient` | `"rich" \| "textarea"` | ❌ | For string fields, render as rich text or textarea |
| `placeholder` | `string` | ❌ | Placeholder text |
| `dateInput` | `DateInputProps` | ❌ | Additional props for date inputs |
| `onChange` | `(value: any, data?: string) => void` | ❌ | Change handler |
| `onBlur` | `() => void` | ❌ | Blur handler |
| `disabled` | `boolean` | ❌ | Disable the field |
| `readOnly` | `boolean` | ❌ | Make field read-only |
| `required` | `boolean` | ❌ | Override required status |
| `allowNegative` | `boolean` | ❌ | Allow negative numbers (numeric fields) |
| `size` | `MantineSize` | ❌ | Field size ("xs" \| "sm" \| "md" \| "lg" \| "xl") |

### Field Type Rendering

The Field component automatically renders based on the entity field type:

| Entity Type | Rendered Component | Notes |
|-------------|-------------------|--------|
| `String` | TextInput / Textarea / RichText | Based on `varient` prop |
| `Guid` | TextInput | UUID format |
| `Int32`, `Int64`, etc. | TextInput | Numeric validation |
| `Single`, `Double`, `Decimal` | TextInput | Decimal validation |
| `Boolean` | Checkbox | |
| `DateTime` | DateInput | With date/time picker |
| `Lookup` | Lookup | Dropdown with search |

### Usage Examples

```tsx
// Basic text field
<Field name="Name" />

// Required field with placeholder
<Field name="Email" required placeholder="user@example.com" />

// Rich text editor
<Field name="Description" varient="rich" />

// Textarea
<Field name="Notes" varient="textarea" />

// Numeric field without negatives
<Field name="Quantity" allowNegative={false} />

// Field with change handler
<Field 
  name="Price" 
  onChange={(value) => console.log("Price changed:", value)}
/>

// Auto-focused field
<Field name="Title" focus />

// Custom label
<Field name="Status" label="Current Status" />
```

### Special Behaviors

1. **Automatic Validation**: Respects entity attributes like `[UIRequired]`, `[UICharacterLimit]`
2. **Number Formatting**: Handles C# numeric types with proper precision
3. **Date Handling**: Removes time component if not in date format
4. **Lookup Fields**: Automatically loads related data from foreign keys
5. **Permission Awareness**: Becomes read-only based on user permissions
6. **Blue Indicator**: Shows "+" for recommended fields (`[UIRecommended]`)

---

## SaveButton

Renders a submit button that saves the form data.

### Import
```tsx
import { SaveButton } from "@ixeta/xams";
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `label` | `string` | ❌ | Button text (defaults to "Create"/"Update") |
| `varient` | `"filled" \| "outline" \| "light" \| "white" \| "default" \| "subtle" \| "gradient"` | ❌ | Button style variant |
| `className` | `string` | ❌ | CSS class names |
| `size` | `MantineSize` | ❌ | Button size ("xs" \| "sm" \| "md" \| "lg" \| "xl") |

### Usage Examples

```tsx
// Default button (shows "Create" or "Update")
<SaveButton />

// Custom label
<SaveButton label="Save Widget" />

// Styled button
<SaveButton varient="gradient" size="lg" />

// With custom CSS
<SaveButton className="mt-4 w-full" />
```

### Features
- Automatically shows "Create" for new records, "Update" for existing
- Hidden when user lacks create/update permissions
- Triggers form validation before submission
- Works as HTML form submit button

---

## useFormBuilder Hook

Core hook for managing form state and operations.

### Import
```tsx
import { useFormBuilder } from "@ixeta/xams";
```

### Parameters

```tsx
interface UseFormBuilderParams {
  tableName: string;                          // Required: Entity name
  id?: string | null;                        // Record ID for updates
  metadata?: MetadataResponse;               // Pre-loaded metadata
  defaults?: FieldValue[];                   // Default field values
  snapshot?: any;                            // Original record data
  lookupExclusions?: LookupExclusions[];    // Lookup filters
  lookupQueries?: LookupQuery[];            // Lookup queries
  canUpdate?: boolean;                       // Override update permission
  canCreate?: boolean;                       // Override create permission
  onPreValidate?: PreSaveEvent;             // Before validation hook
  onPreSave?: PreSaveEvent;                 // Before save hook
  onPostSave?: PostSaveEvent;               // After save hook
  forceShowLoading?: boolean;               // Force loading display
  keepLoadingOnSuccess?: boolean;           // Keep loading after success
}
```

### Returned Object

```tsx
interface useFormBuilderType {
  // Properties
  metadata: MetadataResponse | undefined;
  data: T;                                   // Current form data
  snapshot: T;                               // Original data for updates
  validationMessages: ValidationMessage[];
  isLoading: boolean;
  isSubmitted: boolean;
  operation: "CREATE" | "UPDATE";
  tableName: string;
  canUpdate: boolean;
  canCreate: boolean;
  canRead: { canRead: boolean; message: string };
  
  // Methods
  setSnapshot(snapshot: any, forceShowLoading?: boolean): void;
  reload(reloadDataTables?: boolean): void;
  setField(field: string, value: any): void;
  setFieldError(field: string, message: string): void;
  isDirty(field?: string): boolean;
  save(preValidate?, preSave?, postSave?): void;
  saveSilent(parameters?: any): Promise<void>;
  load(id: string, forceLoading?: boolean): void;
  clearEdits(): void;
  clear(): void;
  validate(): boolean;
  
  // DataTable integration
  addDataTable(dataTable: DataTableRef): void;
  reloadDataTables(): void;
  
  // Required fields management
  addRequiredField(fieldName: string): void;
  removeRequiredField(fieldName: string): void;
}
```

### Usage Examples

```tsx
// Basic create form
const formBuilder = useFormBuilder({
  tableName: "Widget"
});

// Update form with ID
const formBuilder = useFormBuilder({
  tableName: "Widget",
  id: widgetId
});

// Form with defaults
const formBuilder = useFormBuilder({
  tableName: "Widget",
  defaults: [
    { field: "Price", value: 9.99 },
    { field: "Status", value: "active" }
  ]
});

// Form with event handlers
const formBuilder = useFormBuilder({
  tableName: "Widget",
  onPreSave: (operation, data) => {
    if (data.Price < 0) {
      alert("Price cannot be negative");
      return false; // Cancel save
    }
    return true;
  },
  onPostSave: (operation, id) => {
    console.log(`Widget ${operation === "CREATE" ? "created" : "updated"}: ${id}`);
  }
});

// Programmatic field updates
formBuilder.setField("Name", "New Widget");
formBuilder.setField("Price", 19.99);

// Check if form has changes
if (formBuilder.isDirty()) {
  console.log("Form has unsaved changes");
}

// Manual validation
if (formBuilder.validate()) {
  console.log("Form is valid");
}

// Load existing record
formBuilder.load("widget-id-123");

// Save programmatically
await formBuilder.saveSilent();

// Clear form
formBuilder.clear();
```

### Advanced Features

#### Lookup Exclusions
```tsx
const formBuilder = useFormBuilder({
  tableName: "Widget",
  lookupExclusions: [{
    fieldName: "CategoryId",
    values: ["category-1", "category-2"] // Exclude these options
  }]
});
```

#### Lookup Queries
```tsx
const formBuilder = useFormBuilder({
  tableName: "Widget",
  lookupQueries: [{
    field: "StatusId",
    filters: [
      { field: "IsActive", operator: "=", value: true }
    ]
  }]
});
```

#### Silent Save
```tsx
// Save without UI feedback
await formBuilder.saveSilent({ customParam: "value" });
```

#### DataTable Integration
```tsx
// Register DataTable for automatic refresh
formBuilder.addDataTable(dataTableRef);

// Refresh all registered DataTables
formBuilder.reloadDataTables();
```

---

## Complete Form Example

```tsx
import { 
  useFormBuilder, 
  FormContainer, 
  Field, 
  SaveButton 
} from "@ixeta/xams";
import { Grid, Button } from "@mantine/core";

function WidgetForm({ widgetId, onSave }) {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
    id: widgetId,
    defaults: [
      { field: "Status", value: "draft" }
    ],
    onPreValidate: (operation, data) => {
      // Custom validation
      if (!data.Name || data.Name.length < 3) {
        formBuilder.setFieldError("Name", "Name must be at least 3 characters");
        return false;
      }
      return true;
    },
    onPostSave: (operation, id, data) => {
      onSave(id);
    }
  });

  return (
    <FormContainer formBuilder={formBuilder}>
      <Grid>
        <Grid.Col span={12}>
          <Field name="Name" focus required />
        </Grid.Col>
        
        <Grid.Col span={6}>
          <Field name="Price" allowNegative={false} />
        </Grid.Col>
        
        <Grid.Col span={6}>
          <Field name="CategoryId" />
        </Grid.Col>
        
        <Grid.Col span={12}>
          <Field name="Description" varient="textarea" />
        </Grid.Col>
        
        <Grid.Col span={12}>
          <div className="flex gap-2">
            <SaveButton />
            <Button 
              variant="outline" 
              onClick={() => formBuilder.clear()}
            >
              Clear
            </Button>
          </div>
        </Grid.Col>
      </Grid>
    </FormContainer>
  );
}
```

---

← [Back to Main Documentation](../CLAUDE.md) | [Data Components](components-data.md) →