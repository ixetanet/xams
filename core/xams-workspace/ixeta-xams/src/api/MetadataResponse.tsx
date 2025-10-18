export interface MetadataResponse {
  displayName: string;
  tableName: string;
  primaryKey: string;
  fields: MetadataField[];
}

export interface MetadataField {
  name: string;
  displayName: string;
  type: string;
  characterLimit?: number;
  order: number;
  lookupName: string;
  lookupTable: string;
  lookupTableNameField: string;
  lookupTableDescriptionField: string;
  lookupTablePrimaryKeyField: string;
  lookupTableHasActiveField: boolean;
  dateFormat: string;
  isTimeless: boolean;
  isNullable: boolean;
  isRequired: boolean;
  isRecommended: boolean;
  isReadOnly: boolean;
  isCreateOnly: boolean;
  option: string;
  numberRange: string;
  multiSelect?: MetadataMultiSelect;
}

export interface MetadataMultiSelect {
  junctionTable: string;
  junctionOwnerIdField: string;
  junctionTargetIdField: string;
  targetTable: string;
  targetNameField: string;
  targetDescriptionField?: string;
  targetPrimaryKeyField: string;
  targetHasActiveField: boolean;
}
