import React from "react";
import { MantineSize, Textarea, TextareaProps } from "@mantine/core";
import { useFormContext } from "../contexts/FormContext";
import {
  useFieldValue,
  useFieldValidation,
  useFieldPermissions,
  useFieldLabel,
  useFieldRequired,
} from "../hooks/useFieldHelpers";

interface FieldTextareaProps {
  name: string;
  label?: string | React.ReactNode;
  placeholder?: string;
  onChange?: (value: string) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  size?: MantineSize;
  textareaProps?: Omit<TextareaProps, "value" | "onChange" | "onBlur" | "label" | "required" | "disabled" | "readOnly" | "error" | "size" | "placeholder">;
}

const FieldTextarea = (props: FieldTextareaProps) => {
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
    (formContext.formBuilder.canUpdate === false &&
      formContext.formBuilder.snapshot !== undefined);

  const isRequired =
    field.isRequired === true || props.required === true || !field.isNullable;

  const handleChange = (inputValue: string) => {
    let processedValue = inputValue;
    let shouldUpdate = false;

    // Handle character limit
    if (field.characterLimit) {
      if (processedValue.length <= field.characterLimit) {
        shouldUpdate = true;
      } else {
        processedValue = processedValue.substring(0, field.characterLimit);
        shouldUpdate = true;
      }
    } else {
      shouldUpdate = true;
    }

    if (shouldUpdate) {
      setValue(processedValue);
      props.onChange?.(processedValue);
    }
  };

  if (value === undefined) {
    return null;
  }

  return (
    <Textarea
      size={props.size}
      styles={{
        root: {
          height: "100%",
          display: "flex",
          flexDirection: "column",
        },
        wrapper: {
          flexGrow: 1,
        },
        input: {
          height: "100%",
        },
      }}
      label={label}
      placeholder={props.placeholder}
      value={value}
      required={isRequired}
      onChange={(event) => handleChange(event.currentTarget.value)}
      onBlur={props.onBlur}
      readOnly={finalReadOnly}
      error={error}
      disabled={props.disabled}
      {...props.textareaProps}
    />
  );
};

export default FieldTextarea;
