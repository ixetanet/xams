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
  lookupPrimaryKeyField: string;
  dateFormat: string;
  isTimeless: boolean;
  isNullable: boolean;
  isRequired: boolean;
  isRecommended: boolean;
  isReadOnly: boolean;
  option: string;
  numberRange: string;
}
