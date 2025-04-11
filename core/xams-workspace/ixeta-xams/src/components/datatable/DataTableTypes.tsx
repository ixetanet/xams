import {
  ReadExcept,
  ReadFilter,
  ReadJoin,
  ReadOrderBy,
} from "../../api/ReadRequest";
import { useFormBuilderType } from "../../hooks/useFormBuilder";
import {
  FieldValue,
  LookupExclusions,
  LookupQuery,
} from "../../reducers/formbuilderReducer";
import { TableOptions as TableStyle } from "./TableShell";
import {
  DataTableAction,
  DataTableState,
} from "../../reducers/datatableReducer";
import { ReadResponse } from "../../api/ReadResponse";
import { MetadataField, MetadataResponse } from "../../api/MetadataResponse";
import { ApiResponse } from "../../api/ApiResponse";

export type SetDataFunction = (records: any[]) => any[];

export interface DataTableRef {
  refresh: () => void;
  openForm: (recordData: any | undefined) => void;
  dataTableId: string;
  getRecords: () => any[];
  setRecords: (previousData: SetDataFunction) => void;
  showLoading: () => void;
  sort(field: string): void;
}

export type DataTableField = string | DataTableCustomField;

export type DataTableCustomField = {
  header: string | ((refHandle: DataTableRef) => React.ReactNode);
  body: (record: any, refHandle: DataTableRef) => React.ReactNode;
};

type selectable = "single" | "multiple";

export type DataTableFieldInfo = {
  displayName: string | ((refHandle: DataTableRef) => React.ReactNode);
  metadataField?: MetadataField | undefined | null;
  body:
    | undefined
    | null
    | ((record: any, refHandle: DataTableRef) => React.ReactNode);
  width: string;
  alias: string;
};

export interface SelectedRow {
  id: string;
  row: any;
}

export type DataTableProps = {
  title?: string;
  disabledMessage?: string;
  confirmDelete?: boolean;

  tableName: string;
  maxResults?: number;
  fields?: DataTableField[];
  additionalFields?: string[]; // Additional fields to query
  orderBy?: ReadOrderBy[]; // Default order by
  filters?: ReadFilter[]; // Default filters to apply
  joins?: ReadJoin[];
  except?: ReadExcept[];

  scrollable?: boolean;
  searchable?: boolean;
  selectable?: selectable;
  columnWidths?: string[];
  showActiveSwitch?: boolean;
  showOptions?: boolean; // Show \ Hide Import\Export options, etc.
  deleteConfirmation?: (record: any) => Promise<{
    title?: string;
    message?: string;
    showPrompt?: boolean;
  }>;
  pagination?: boolean;

  formTitle?: string;
  formFields?: string[]; // What fields to show in the form
  formFieldDefaults?: FieldValue[]; // Default values for the form even if hidden
  formLookupExclusions?: LookupExclusions[]; // What lookup values to exclude from the form
  formLookupQueries?: LookupQuery[];
  formCloseOnCreate?: boolean; // Close the form on create
  formCloseOnUpdate?: boolean; // Close the form on update
  formCloseOnEscape?: boolean; // Close the form on escape
  formZIndex?: number; // Z index of the form
  formMaxWidth?: number; // Maximum width of the form
  formMinWidth?: number; // Minimum width of the form
  formHideSaveButton?: boolean; // Hide the submit button
  formOnClose?: () => void; // Callback when the form is closed
  formOnOpen?: (operation: "CREATE" | "UPDATE", record: any) => void; // Callback when the form is opened
  formOnPreSave?: (submissionData: any, parameters?: any) => void;
  formOnPostSave?: (operation: "CREATE" | "UPDATE", record: any) => void;
  refreshInterval?: number; // How often to refresh the data in milliseconds
  customForm?: (
    formbuilder: useFormBuilderType,
    disclosure: FormDisclosure
  ) => React.ReactNode | React.ReactElement | JSX.Element;
  appendCustomForm?: (formbuilder: useFormBuilderType) => React.ReactNode;
  formAppendButton?: (formbuilder: useFormBuilderType) => React.ReactNode;
  customCreateButton?: (
    openForm: () => void
  ) => React.ReactNode | React.ReactElement | JSX.Element;
  customRow?: (record: any) => React.ReactNode;
  onInitialLoad?: (results: any[]) => void;
  onPostDelete?: (record: any) => void;
  onRowClick?: (record: any) => boolean; // Return true to open the form
  onPageChange?: (page: number) => void;
  onDataLoaded?: (data: ReadResponse<any>) => void;
  canDeactivate?: boolean; // If true, will show the deactivate button
  canDelete?: boolean;
  canUpdate?: boolean; // If true or undefined, will use the users permissions to show\hide the update button
  canCreate?: boolean;
  canImport?: boolean;
  canExport?: boolean;
  tableStyle?: TableStyle;
};

export const getDataOptions = {
  page: 1,
  fields: null,
  orderBy: null,
  searchField: null,
  searchValue: null,
  setData: null,
  active: null,
} as GetDataOptions;

export interface GetDataOptions {
  page: number;
  fields: DataTableField[] | null;
  orderBy?: ReadOrderBy[] | null;
  searchField: string | null;
  searchValue: string | null;
  setData: boolean | null;
  active?: boolean | null;
  showLoading?: boolean;
  metadata?: MetadataResponse;
  joinMetadata?: MetadataResponse[];
}

interface FormDisclosure {
  opened: boolean;
  close: () => void;
}

export interface DataTableShape {
  props: DataTableProps;
  state: DataTableState;
  refHandle: DataTableRef;
  dispatch: React.Dispatch<DataTableAction>;
  formDisclosure: FormDisclosure;
  openForm: (recordData: any | undefined, triggerOnRowClick?: boolean) => void;
  closeForm: () => void;
  refresh: () => Promise<void>;
  getData: (
    options?: GetDataOptions | null
  ) => Promise<ApiResponse<ReadResponse<any>>>;
  getFields: () => DataTableFieldInfo[];
  sort: (field: string) => void;
}
