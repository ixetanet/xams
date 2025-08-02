import { MetadataResponse } from "../api/MetadataResponse";
import { ReadResponse } from "../api/ReadResponse";
import { SetDataFunction } from "../components/datatable/DataTableTypes";
import {
  PermissionLevel,
  TablePermissions,
} from "../stores/usePermissionStore";

export type DataTableTypes =
  | "START_INITIAL_LOAD"
  | "MISSING_READ_PERMISSIONS"
  | "TABLE_NOT_FOUND"
  | "INITIAL_LOAD_COMPLETE"
  | "SET_IS_LOADING"
  | "SET_DONE_LOADING"
  | "SEARCH_VALUE_CHANGE"
  | "ACTIVE_SWITCH_CHANGE"
  | "SET_METADATA"
  | "SET_DATA"
  | "OPEN_FORM"
  | "CLOSE_FORM"
  | "SET_VISIBLE_FIELDS";

export interface DataTableState {
  type: DataTableTypes;
  id: string;
  isLoadingData?: boolean;
  permissions: TablePermissions;
  isTableNotFound?: boolean;
  isFormOpen: boolean;
  metadata?: MetadataResponse | undefined;
  joinMetadata: MetadataResponse[];
  data: ReadResponse<any>;
  searchField?: string | null;
  searchValue?: string | null;
  activeSwitch?: string | null;
  editRecordId?: string | null;
  formTableName?: string; // This is only set when the form is open to prevent loading without a table name and id
  visibleFields?: string[]; // When the user has selected specific fields to show
}

interface TypeInitialLoad {
  metadata: MetadataResponse;
  joinMetadata: MetadataResponse[];
  permissions: TablePermissions;
  data: ReadResponse<any>;
  isLoadingData: boolean;
  activeSwitch: string | null;
}

interface TypeSearchValue {
  searchValue: string | null;
  searchField: string | null;
  data: ReadResponse<any>;
}

interface TypeRowHoverId {
  rowHoverId: string | undefined;
}

interface TypeActiveSwitch {
  activeSwitch: string | null;
}

interface TypeSetMetadata {
  metadata: MetadataResponse;
  joinMetadata: MetadataResponse[];
}

interface TypeSetData {
  setDataFunction: SetDataFunction;
}

interface TypeOpenForm {
  editRecordId: string | null;
}

interface TypeVisibleFields {
  visibleFields: string[];
}

const emptyReadResponse = {
  pages: 0,
  currentPage: 1,
  totalResults: 0,
  orderBy: [],
  tableName: "",
  maxResults: 10,
  results: [],
  parameters: {},
} as ReadResponse<any>;

export const dataTableInitState = {
  type: "START_INITIAL_LOAD",
  id: "",
  isLoadingData: true,
  permissions: {
    read: "NONE",
    create: "NONE",
    delete: "NONE",
    update: "NONE",
  },
  isTableNotFound: false,
  isFormOpen: false,
  metadata: undefined,
  joinMetadata: [],
  data: emptyReadResponse,
  searchField: "",
  searchValue: "",
  activeSwitch: "Active",
  editRecordId: null,
  visibleFields: undefined,
} as DataTableState;

export interface DataTableAction {
  type: DataTableTypes;
  payload?:
    | DataTableState
    | TypeInitialLoad
    | ReadResponse<any>
    | TypeSearchValue
    | TypeRowHoverId
    | TypeActiveSwitch
    | TypeSetMetadata
    | TypeSetData
    | TypeOpenForm
    | TypeVisibleFields;
}

export const datatableReducer = (
  state: DataTableState,
  action: DataTableAction
): DataTableState => {
  console.log("DataTable: " + action.type);
  switch (action.type) {
    case "START_INITIAL_LOAD":
      return {
        ...dataTableInitState,
        isLoadingData: true,
        visibleFields: state.visibleFields, // Keep the visible fields
        type: "START_INITIAL_LOAD",
      };
    case "MISSING_READ_PERMISSIONS":
      return {
        ...dataTableInitState,
        isLoadingData: false,
        permissions: {
          ...state.permissions,
          read: "NONE",
        },
        type: "MISSING_READ_PERMISSIONS",
      };
    case "TABLE_NOT_FOUND":
      return {
        ...dataTableInitState,
        isLoadingData: false,
        isTableNotFound: true,
        type: "TABLE_NOT_FOUND",
      };
    case "INITIAL_LOAD_COMPLETE":
      return {
        ...dataTableInitState,
        ...(action.payload as TypeInitialLoad),
        // If the edit record was set while the datatable was loading,
        // change to type OPEN_FORM to open the form
        editRecordId: state.editRecordId,
        type: state.editRecordId ? "OPEN_FORM" : "INITIAL_LOAD_COMPLETE",
        visibleFields: state.visibleFields,
      };
    case "SET_IS_LOADING":
      return {
        ...state,
        isLoadingData: true,
        type: "SET_IS_LOADING",
      };
    case "SET_DONE_LOADING":
      return {
        ...state,
        isLoadingData: false,
        data: action.payload as ReadResponse<any>,
        type: "SET_DONE_LOADING",
      };
    case "SEARCH_VALUE_CHANGE":
      return {
        ...state,
        searchValue: (action.payload as TypeSearchValue).searchValue,
        searchField: (action.payload as TypeSearchValue).searchField,
        data: (action.payload as TypeSearchValue).data,
        type: "SEARCH_VALUE_CHANGE",
      };
    case "ACTIVE_SWITCH_CHANGE":
      return {
        ...state,
        activeSwitch: (action.payload as TypeActiveSwitch).activeSwitch,
        type: "ACTIVE_SWITCH_CHANGE",
      };
    case "SET_METADATA":
      return {
        ...state,
        metadata: (action.payload as TypeSetMetadata).metadata,
        joinMetadata: (action.payload as TypeSetMetadata).joinMetadata,
        type: "SET_METADATA",
      };
    case "SET_DATA":
      return {
        ...state,
        data: {
          ...state.data,
          results: (action.payload as TypeSetData).setDataFunction(
            state.data.results
          ),
        },
        type: "SET_DATA",
      };
    case "OPEN_FORM":
      return {
        ...state,
        isFormOpen: true,
        editRecordId: (action.payload as TypeOpenForm).editRecordId,
        formTableName: state.metadata?.tableName,
        type: "OPEN_FORM",
      };
    case "CLOSE_FORM":
      return {
        ...state,
        isFormOpen: false,
        editRecordId: undefined,
        formTableName: undefined,
        type: "CLOSE_FORM",
      };

    case "SET_VISIBLE_FIELDS":
      return {
        ...state,
        visibleFields: (action.payload as TypeVisibleFields).visibleFields,
        type: "SET_VISIBLE_FIELDS",
      };
  }

  return { ...state, ...action.payload };
};
