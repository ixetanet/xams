import React from "react";
import { MantineSize } from "@mantine/core";
import { DateInput, DateInputProps } from "@mantine/dates";
import { useFormContext } from "../contexts/FormContext";
import {
  useFieldValue,
  useFieldValidation,
  useFieldPermissions,
  useFieldLabel,
  useFieldRequired,
} from "../hooks/useFieldHelpers";
import { hasTimePart } from "../utils/Util";

interface FieldDateProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  onChange?: (value: string | null) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  size?: MantineSize;
  dateInputProps?: Omit<DateInputProps, "value" | "onChange" | "onBlur" | "label" | "required" | "disabled" | "readOnly" | "error" | "size" | "clearable">;
}

const FieldDate = (props: FieldDateProps) => {
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
    ["CreatedDate", "UpdatedDate"].includes(field.name) ||
    (formContext.formBuilder.snapshot !== undefined &&
      formContext.formBuilder.canUpdate === false);

  const isRequired =
    field.isRequired === true || props.required || !field.isNullable;

  const handleChange = (date: string | null) => {
    const processedValue =
      date == null
        ? null
        : !hasTimePart(field.dateFormat)
        ? date + "T00:00:00" // Add time component for date-only fields
        : date;

    setValue(processedValue);
    props.onChange?.(processedValue);
  };

  if (value === undefined) {
    return null;
  }

  // Process the display value
  const displayValue =
    value !== null && value !== "0001-01-01T00:00:00"
      ? !hasTimePart(field.dateFormat)
        ? value.replace("Z", "").split("T")[0] // Extract date only
        : value.replace("Z", "")
      : null;

  return (
    <DateInput
      ref={
        props.focus === true ? formContext.formBuilder.firstInputRef : undefined
      }
      size={props.size}
      label={label}
      {...(field.dateFormat != null && field.dateFormat !== ""
        ? { valueFormat: field.dateFormat }
        : {})}
      value={displayValue}
      onChange={handleChange}
      onBlur={props.onBlur}
      clearable={field.isNullable}
      readOnly={finalReadOnly}
      required={isRequired}
      error={error}
      disabled={props.disabled}
      {...props.dateInputProps}
    />
  );
};

export default FieldDate;
