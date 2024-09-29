import { MetadataResponse } from "../api/MetadataResponse";
import { ReadExcept, ReadFilter, ReadJoin } from "../api/ReadRequest";

export type FormBuilderType =
  | "INSTANTIATED"
  | "START_INITIAL_LOAD"
  | "INITIAL_LOAD_COMPLETE"
  | "SET_FIELD_VALUE"
  | "SET_IS_LOADING"
  | "SUBMIT_COMPLETE"
  | "SUBMIT_CANCELLED"
  | "SET_VALIDATION_MESSAGES"
  | "SET_VALIDATION_MESSAGE"
  | "SET_DATA_TO_EDIT"
  | "CLEAR_EDIT_DATA";

export interface ValidationMessage {
  field: string;
  message: string;
}

export interface FieldValue {
  field: string;
  operator?: string;
  value: string | boolean | number | null;
}

export interface LookupExclusions {
  fieldName: string;
  values: string[] | FieldValue[];
}

export interface LookupQuery {
  field: string;
  orderBy?: string;
  order?: string;
  filters?: ReadFilter[];
  joins?: ReadJoin[];
  except?: ReadExcept[];
}

interface FormBuilderState<T> {
  type: string;
  tableName: string | undefined;
  data: T;
  snapshot: T;
  dirtyFields: string[];
  validationMessages: ValidationMessage[];
  metadata: MetadataResponse | undefined;
  canUpdate: boolean;
  canCreate: boolean;
  canRead: {
    canRead: boolean;
    message: string;
  };
  isLoading: boolean;
  forceIsLoading: boolean; // Force the form to be in a loading state
  isSubmitted: boolean; // Has the form been saved (create or updated)
}

export const getFormBuilderInitState = <T,>() => {
  return {
    type: "INSTANTIATED",
    tableName: undefined,
    data: undefined,
    snapshot: undefined,
    dirtyFields: [],
    validationMessages: [],
    metadata: undefined,
    canUpdate: true,
    canCreate: true,
    canRead: {
      canRead: true,
      message: "",
    },
    isLoading: true,
    forceIsLoading: false,
    isSubmitted: false,
    eventListeners: [],
  } as FormBuilderState<T | undefined>;
};

interface TypeLoadComplete<T> {
  data: T;
  snapshot: T;
  metadata: MetadataResponse;
  canUpdate: boolean;
  canCreate: boolean;
  canRead: {
    canRead: boolean;
    message: string;
  };
}

interface TypeSetFieldValue {
  field: string;
  value: string | boolean | null | undefined | number;
}

interface TypeShowForceLoading {
  forceIsLoading: boolean;
}

interface TypeDataToEdit<T> {
  data: T;
  snapshot: T;
  canUpdate?: boolean;
  forceIsLoading?: boolean;
}

interface TypeInitialLoad<T> {
  tableName: string;
  data: T;
  forceIsLoading?: boolean;
}

export interface FormBuilderAction<T> {
  type: string;
  payload?:
    | FormBuilderState<T>
    | TypeLoadComplete<T>
    | TypeSetFieldValue
    | TypeDataToEdit<T>
    | TypeInitialLoad<T>
    | ValidationMessage[]
    | TypeShowForceLoading
    | ValidationMessage;
}

export const formbuilderReducer = <T,>(
  state: FormBuilderState<T>,
  action: FormBuilderAction<T>
): FormBuilderState<T | undefined> => {
  console.log("FormBuilder: " + action.type);
  switch (action.type) {
    case "START_INITIAL_LOAD":
      return {
        ...getFormBuilderInitState<T>(),
        data: (action.payload as TypeInitialLoad<T>).data,
        snapshot: undefined,
        metadata: state.metadata,
        isLoading: true,
        forceIsLoading:
          (action.payload as TypeInitialLoad<T>).forceIsLoading ?? false,
        type: "START_INITIAL_LOAD",
      };
    case "INITIAL_LOAD_COMPLETE":
      return {
        ...state,
        ...(action.payload as TypeLoadComplete<T>),
        isLoading: false,
        dirtyFields: [],
        type: "INITIAL_LOAD_COMPLETE",
      };
    case "SET_FIELD_VALUE":
      const fieldName = (action.payload as TypeSetFieldValue).field;
      return {
        ...state,
        ...(state.dirtyFields.includes(fieldName)
          ? { dirtyFields: [...state.dirtyFields] }
          : { dirtyFields: [...state.dirtyFields, fieldName] }),
        data: {
          ...state.data,
          [fieldName]: (action.payload as TypeSetFieldValue).value,
        },
        type: "SET_FIELD_VALUE",
      };
    case "SET_IS_LOADING":
      return {
        ...state,
        isLoading: true,
        type: "SET_IS_LOADING",
      };
    case "SUBMIT_COMPLETE":
      return {
        ...state,
        isLoading: false,
        isSubmitted: true,
        validationMessages: [],
        dirtyFields: [],
        type: "SUBMIT_COMPLETE",
      };
    case "SUBMIT_CANCELLED":
      return {
        ...state,
        isLoading: false,
        type: "SUBMIT_CANCELLED",
      };
    case "SET_VALIDATION_MESSAGES":
      return {
        ...state,
        validationMessages: action.payload as ValidationMessage[],
        type: "SET_VALIDATION_MESSAGES",
      };
    case "SET_VALIDATION_MESSAGE":
      const validationMessage = action.payload as ValidationMessage;
      return {
        ...state,
        validationMessages: [
          ...state.validationMessages.filter(
            (msg) => msg.field !== validationMessage.field
          ),
          validationMessage,
        ],
        type: "SET_VALIDATION_MESSAGE",
      };
    case "SET_DATA_TO_EDIT":
      const typeDataToEdit = action.payload as TypeDataToEdit<T>;
      return {
        ...state,
        validationMessages: [],
        snapshot: (action.payload as TypeDataToEdit<T>).snapshot,
        data: (action.payload as TypeDataToEdit<T>).data,
        canUpdate:
          (action.payload as TypeDataToEdit<T>).data == null ||
          (typeDataToEdit.canUpdate != null &&
            typeDataToEdit.canUpdate == false)
            ? false
            : (typeDataToEdit as any)?.data["_ui_info_"].canUpdate ?? false,
        forceIsLoading: typeDataToEdit.forceIsLoading ?? false,
        type: "SET_DATA_TO_EDIT",
      };
    case "CLEAR_EDIT_DATA":
      return {
        ...state,
        type: "CLEAR_EDIT_DATA",
        data: state.snapshot,
      };
    case "CLEAR":
      return {
        ...state,
        type: "CLEAR",
        data: undefined,
        snapshot: undefined,
      };
    case "SET_SHOW_FORCE_LOADING":
      return {
        ...state,
        forceIsLoading: (action.payload as TypeShowForceLoading).forceIsLoading,
        type: "SET_SHOW_FORCE_LOADING",
      };
  }
  return { ...state };
};
