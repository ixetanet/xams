import React from "react";
import { MantineSize } from "@mantine/core";
import { useFormContext } from "../contexts/FormContext";
import {
  useFieldValue,
  useFieldValidation,
  useFieldPermissions,
  useFieldLabel,
  useFieldRequired,
} from "../hooks/useFieldHelpers";
import Lookup from "./Lookup";
import { LookupQuery } from "../reducers/formbuilderReducer";

export interface LookupValue {
  id: string | null;
  value: string | null;
  label: string | null;
}

interface FieldLookupProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  onChange?: (value: LookupValue | null) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  size?: MantineSize;
  excludeValues?: string[];
  query?: LookupQuery;
}

const FieldLookup = (props: FieldLookupProps) => {
  const formContext = useFormContext();
  const field = formContext.formBuilder.metadata?.fields.find(
    (x) => x.name === props.name
  );

  // If the metadata hasn't loaded yet
  if (formContext.formBuilder.metadata == null) {
    return <></>;
  }

  if (field == null) {
    throw Error(
      `Field ${props.name} not found on ${formContext.formBuilder.metadata.tableName} metadata.`
    );
  }

  const [value, setValue] = useFieldValue(props.name);
  const error = useFieldValidation(props.name);
  const { isReadOnly } = useFieldPermissions(field);
  const label = useFieldLabel(field, props.label);

  useFieldRequired(props.name, props.required);

  const finalReadOnly =
    props.readOnly ||
    isReadOnly ||
    ["CreatedById", "UpdatedById"].includes(field.name) ||
    (formContext.formBuilder.canUpdate === false &&
      formContext.formBuilder.snapshot !== undefined) ||
    (["OwningUserId", "OwningTeamId"].includes(field.name) &&
      formContext.formBuilder.snapshot !== undefined &&
      formContext.formBuilder.snapshot["_ui_info_"].canAssign === false);

  const isRequired =
    (field.isNullable === false &&
      !["CreatedById", "UpdatedById"].includes(field.name)) ||
    field.isRequired === true ||
    props.required === true;

  const getEditLookupItem = (label: string | null, value: string | null) => {
    if (label == null && value != null) {
      return {
        label: value,
        value: value,
      };
    }

    if (label == null || value == null) {
      return undefined;
    }

    return {
      label: label,
      value: value,
    };
  };

  const getDefaultLookupItem = (
    fieldName: string
  ): { label: string; value: string } | undefined => {
    const fieldDefault = formContext.formBuilder.defaults?.find(
      (x) => x.field === fieldName
    );
    if (fieldDefault !== undefined) {
      return {
        label: "",
        value: fieldDefault.value as string,
      };
    }
    return undefined;
  };

  const handleChange = (lookupValue: LookupValue | null) => {
    setValue(lookupValue?.id);
    props.onChange?.(lookupValue);
  };

  if (value === undefined) {
    return null;
  }

  return (
    <Lookup
      label={label}
      size={props.size}
      className={
        formContext.formBuilder.canRead.canRead === false ? "invisible" : ""
      }
      metaDataField={field}
      owningTableName={formContext.formBuilder.metadata?.tableName as string}
      ref={
        props.focus === true ? formContext.formBuilder.firstInputRef : undefined
      }
      onChange={(lookupValue) => {
        const typedValue: LookupValue | null = lookupValue
          ? {
              id: lookupValue.id,
              value: lookupValue.value,
              label: lookupValue.label,
            }
          : null;
        handleChange(typedValue);
      }}
      onBlur={props.onBlur}
      defaultLabelValue={
        formContext.formBuilder.snapshot !== undefined
          ? getEditLookupItem(
              formContext.formBuilder.snapshot[field.lookupName],
              formContext.formBuilder.snapshot[field.name]
            )
          : getDefaultLookupItem(field.name)
      }
      value={value}
      excludeValues={
        props.excludeValues ||
        (formContext.formBuilder.lookupExclusions?.find(
          (x) => x.fieldName === field.name
        )?.values as string[])
      }
      query={
        props.query ||
        (formContext.formBuilder.lookupQueries?.find(
          (x) => x.field === field.name
        ) as LookupQuery)
      }
      readOnly={finalReadOnly}
      required={isRequired}
      error={error}
      disabled={props.disabled}
    />
  );
};

export default FieldLookup;
