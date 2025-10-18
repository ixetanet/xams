# @ixeta/xams - API Signatures

**Version**: 1.0.16
**Auto-generated**: 2025-10-12

This document contains concise TypeScript signatures for all exported items from @ixeta/xams.

---

## Hooks

### useAuthRequest

```typescript
interface useAuthRequestProps {}
```

### useFormBuilder

```typescript
interface useFormBuilderProps {
  tableName: string;
  id?: string | null;
  metadata?: MetadataResponse;
  defaults?: FieldValue[];
  snapshot?: any;
  lookupExclusions?: LookupExclusions[];
  lookupQueries?: LookupQuery[];
  canUpdate?: boolean;
  canCreate?: boolean;
  onPreValidate?: PreSaveEvent;
  onPreSave?: PreSaveEvent;
  onPostSave?: PostSaveEvent;
  forceShowLoading?: boolean;
  keepLoadingOnSuccess?: boolean;
}
```

### \*

```typescript
export type SaveEventResponse = {
export type PreSaveEvent = (submissionData: any) => Promise<SaveEventResponse>;
export type PostSaveEvent = (operation: "CREATE" | "UPDATE" | "FAILED", id: string, data: any) => void;
export type useFormBuilderType<T = any> = ReturnType<typeof useFormBuilder<T>>;
export default useFormBuilder;
```

### useColor

```typescript
declare const useColor: () => {
export default useColor;
```

### useAuthRequestType

```typescript
export type useAuthRequestType = ReturnType<typeof useAuthRequest>;
```

### useFormBuilderType

```typescript
export type useFormBuilderType<T = any> = ReturnType<typeof useFormBuilder<T>>;
```

## Components

### DataTable

```typescript
declare const DataTable: React.ForwardRefExoticComponent<
  DataTableProps & React.RefAttributes<unknown>
>;
export default DataTable;
```

### DataTableSelectable

```typescript
declare const DataTableSelectable: (props: DataTableProps & {
export default DataTableSelectable;
```

### \*

```typescript
export type SetDataFunction = (records: any[]) => any[];
export interface DataTableRef {
export type DataTableField = string | DataTableCustomField;
export type DataTableCustomField = {
export type DataTableFieldInfo = {
export interface SelectedRow {
export type DataTableProps = {
export declare const getDataOptions: GetDataOptions;
export interface GetDataOptions {
export interface DataTableShape {
export {};
```

### DataGrid

```typescript
export interface DataGridRef {
  reset: () => void;
  activeCell?: CellLocation;
  setActiveCell: (cell: CellLocation) => void;
  isEditing?: boolean;
  editValue?: string;
  setEditValue: (value: string) => void;
}
```

### DataGridRef

```typescript
export interface DataGridRef {
  reset: () => void;
  activeCell?: CellLocation;
  setActiveCell: (cell: CellLocation) => void;
  isEditing?: boolean;
  editValue?: string;
  setEditValue: (value: string) => void;
}
```

### \*

```typescript
export interface DataGridProps {
export interface CellLocation {
export interface Cell {
export interface Row {
```

### FormContainer

```typescript
interface FormContainerProps {
  formBuilder: useFormBuilderType;
  className?: string;
  children?: any;
  showLoading?: boolean;
  onPreValidate?: PreSaveEvent;
  onPreSave?: PreSaveEvent;
  onPostSave?: PostSaveEvent;
}
```

### Field

```typescript
interface FieldProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  varient?: "rich" | "textarea";
  placeholder?: string;
  dateInput?: DateInputProps;
  onChange?: (
    value: string | boolean | null | undefined,
    data?: string | null
  ) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  allowNegative?: boolean;
  size?: MantineSize;
}
```

### SaveButton

```typescript
interface SaveButtonProps {
  label?: string;
  varient?:
    | "filled"
    | "outline"
    | "light"
    | "white"
    | "default"
    | "subtle"
    | "gradient";
  className?: string;
  size?: MantineSize;
}
```

### ToggleMode

```typescript
interface ToggleModeProps {
  darkColor?: string;
  lightColor?: string;
}
```

## Contexts

### useAdminDashContext

```typescript
export declare const useAdminDashContext: () => AdminDashContextShape;
```

### AdminDashboardProps

```typescript
export interface AdminDashboardProps {
  title?: string;
  visibleEntities?: string[];
  showEntityDisplayNames?: boolean;
  addMenuItems?: NavItem[];
  hiddenEntities?: string[];
  hiddenMenuItems?: string[];
  forceHideImportData?: boolean;
  forceHideExportData?: boolean;
  forceHideToggleMode?: boolean;
  userCard?: ReactNode;
  accessDeniedMessage?: ReactNode;
}
```

### NavItem

```typescript
export interface NavItem {
  order: number;
  navLink: React.JSX.Element;
}
```

### AdminDashContextShape

```typescript
export type AdminDashContextShape = {
  props: AdminDashboardProps;
  tables: TablesResponse[];
  color: string;
  setActiveComponent: React.Dispatch<
    React.SetStateAction<
      | {
          component: React.ReactNode;
        }
      | undefined
    >
  >;
  emptyTableInfo: any;
};
```

### default

```typescript
export default AppContextProvider;
```

### useAppContext

```typescript
export declare const useAppContext: () => AppContextShape;
```

### AuthContextProvider

```typescript
export interface AuthContextProviderProps {
  onUnauthorized?: () => void;
  apiUrl: string;
  headers?: {
    [key: string]: string;
  };
  children?: any;
  withCredentials?: boolean;
  getAccessToken?: () => Promise<string | undefined>;
}
```

### useAuthContext

```typescript
export declare const useAuthContext: () => AuthContextShape;
```

### useFormContext

```typescript
export declare const useFormContext: () => FormContextShape;
```

## API Types

### ReadFilter

```typescript
export interface ReadFilter {
  logicalOperator?: "AND" | "OR";
  filters?: ReadFilter[];
  field?: string;
  value?: string | null;
  operator?: "==" | "!=" | ">" | ">=" | "<" | "<=" | "contains";
}
```

### ApiResponse

```typescript
export interface ApiResponse<T> {
  succeeded: boolean;
  data: T;
  friendlyMessage: string;
  logMessage: string;
  response: Response | undefined;
}
```

### [All exports from ./stores/usePermissionStore]

```typescript
export declare const readPermisions = "TABLE_{tableName}_READ_SYSTEM, TABLE_{tableName}_READ_TEAM, TABLE_{tableName}_READ_USER";
export declare const createPermissions = "TABLE_{tableName}_CREATE_SYSTEM, TABLE_{tableName}_CREATE_TEAM, TABLE_{tableName}_CREATE_USER";
export declare const deletePermissions = "TABLE_{tableName}_DELETE_SYSTEM, TABLE_{tableName}_DELETE_TEAM, TABLE_{tableName}_DELETE_USER";
export declare const updatePermissions = "TABLE_{tableName}_UPDATE_SYSTEM, TABLE_{tableName}_UPDATE_TEAM, TABLE_{tableName}_UPDATE_USER";
export interface TablePermissions {
export type PermissionLevel = "SYSTEM" | "TEAM" | "USER" | "NONE";
export interface TablePermission {
export interface usePermissionStoreState {
export declare const usePermissionStore: import("zustand").UseBoundStore<import("zustand").StoreApi<usePermissionStoreState>>;
export type usePermissionStoreType = ReturnType<typeof usePermissionStore>;
export default usePermissionStore;
```

### \*

```typescript
export interface TablesResponse {
```

### \*

```typescript
export interface ReadRequest {
export interface ReadJoin {
export interface ReadFilter {
export interface ReadExcept {
export interface ReadOrderBy {
```

### \*

```typescript
export type ReadResponse<T, U = any> = {
```

## Utilities

### \*

```typescript
export declare class Query {
export declare const exp: (field: string, operator: operators, value: any) => Filter;
export default Query;
```

## Other Exports

### AdminDashboard

```typescript
declare const AdminDashboard: (props: AdminDashboardProps) => React.JSX.Element;
export default AdminDashboard;
```

### RolePermissionState

```typescript
export type RolePermissionState = {
  tables: TablesResponse[];
  allPermissions: Permission[];
  rolePermissions: RolePermission[];
  createRolePermissions: RolePermission[];
  updateRolePermissions: RolePermission[];
  deleteRolePermissions: RolePermission[];
  isLoaded: boolean;
};
```

### SystemAdministratorRoleId

```typescript
export declare const SystemAdministratorRoleId =
  "64589861-0481-4dbb-a96f-9b8b6546c40d";
```

### getQueryParam

```typescript
export declare function getQueryParam(
  name: string,
  url?: string
): string | null;
```

### addUserIdUrlParam

```typescript
export declare function addUserIdUrlParam(
  currentUrl: string,
  destinationUrl: string
): string;
```

### API_DATA_PERMISSIONS

```typescript
export declare const API_DATA_PERMISSIONS = "/xams/Permissions";
```

### API_DATA_ACTION

```typescript
export declare const API_DATA_ACTION = "/xams/Action";
```

### API_DATA_CREATE

```typescript
export declare const API_DATA_CREATE = "/xams/Create";
```

### API_DATA_READ

```typescript
export declare const API_DATA_READ = "/xams/Read";
```

### API_DATA_UPDATE

```typescript
export declare const API_DATA_UPDATE = "/xams/Update";
```

### API_DATA_DELETE

```typescript
export declare const API_DATA_DELETE = "/xams/Delete";
```

### API_DATA_FILE

```typescript
export declare const API_DATA_FILE = "/xams/File";
```

### API_DATA_METADATA

```typescript
export declare const API_DATA_METADATA = "/xams/MetaData";
```

### API_CONFIG

```typescript
export declare const API_CONFIG = "/xams/Config";
```
