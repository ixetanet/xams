import React from "react";
import { MantineSize } from "@mantine/core";
import { DateInputProps } from "@mantine/dates";
import { useFormContext } from "../contexts/FormContext";
import FieldText from "./FieldText";
import FieldTextarea from "./FieldTextarea";
import FieldDate from "./FieldDate";
import FieldBoolean from "./FieldBoolean";
import FieldLookup, { LookupValue } from "./FieldLookup";
import FieldMultiSelect, { MultiSelectValue } from "./FieldMultiSelect";

interface FieldProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  variant?: "rich" | "textarea";
  placeholder?: string;
  dateInput?: DateInputProps;
  onChange?: (
    value:
      | Array<{ id: string; name: string }>
      | string[]
      | string
      | boolean
      | null
      | undefined,
    data?: string | null
  ) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  allowNegative?: boolean;
  size?: MantineSize;
}

const Field = (props: FieldProps) => {
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

  // Check for removed rich text variant
  if (props.variant === "rich") {
    throw Error(
      `Field component no longer supports varient="rich". Use <FieldRichText name="${props.name}" /> instead.`
    );
  }

  // Common props to pass to all field types
  const commonProps = {
    name: props.name,
    label: props.label,
    focus: props.focus,
    onBlur: props.onBlur,
    disabled: props.disabled,
    readOnly: props.readOnly,
    required: props.required,
    size: props.size,
  };

  // Route to appropriate Field* component based on field type
  if (field.type === "Lookup") {
    return (
      <FieldLookup
        {...commonProps}
        onChange={(value: LookupValue | null) => {
          // Backward compatibility: call onChange with id and label
          if (props.onChange) {
            props.onChange(value?.id, value?.label);
          }
        }}
      />
    );
  }

  if (field.type === "MultiSelect") {
    return (
      <FieldMultiSelect
        {...commonProps}
        onChange={(values: Array<MultiSelectValue>) => {
          // Backward compatibility: call onChange with values array
          if (props.onChange) {
            props.onChange(values);
          }
        }}
      />
    );
  }

  if (field.type === "String" && props.variant === "textarea") {
    return (
      <FieldTextarea
        {...commonProps}
        placeholder={props.placeholder}
        onChange={(value: string) => {
          if (props.onChange) {
            props.onChange(value);
          }
        }}
      />
    );
  }

  if (field.type === "DateTime") {
    return (
      <FieldDate
        {...commonProps}
        dateInputProps={props.dateInput}
        onChange={(value: string | null) => {
          if (props.onChange) {
            props.onChange(value);
          }
        }}
      />
    );
  }

  if (field.type === "Boolean") {
    return (
      <FieldBoolean
        {...commonProps}
        onChange={(checked: boolean) => {
          if (props.onChange) {
            props.onChange(checked);
          }
        }}
      />
    );
  }

  // All other types (String, Guid, numeric types, Char) use FieldText
  const textTypes = [
    "String",
    "Guid",
    "Int32",
    "Single",
    "Int64",
    "Double",
    "Decimal",
    "Byte",
    "SByte",
    "UInt32",
    "UInt64",
    "Int16",
    "UInt16",
    "Char",
  ];

  if (textTypes.includes(field.type)) {
    return (
      <FieldText
        {...commonProps}
        placeholder={props.placeholder}
        allowNegative={props.allowNegative}
        onChange={(value: string) => {
          if (props.onChange) {
            props.onChange(value);
          }
        }}
      />
    );
  }

  // Fallback: unsupported field type
  console.warn(`Unsupported field type: ${field.type} for field ${props.name}`);
  return null;
};

export default Field;
