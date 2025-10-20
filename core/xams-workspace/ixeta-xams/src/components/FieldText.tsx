import React from "react";
import { MantineSize, TextInput, TextInputProps } from "@mantine/core";
import { useFormContext } from "../contexts/FormContext";
import {
  useFieldValue,
  useFieldValidation,
  useFieldPermissions,
  useFieldLabel,
  useFieldRequired,
} from "../hooks/useFieldHelpers";
import {
  CSNumericType,
  parseFloatCS,
  parseIntCS,
} from "../utils/CsNumberTypes";

interface FieldTextProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  placeholder?: string;
  onChange?: (value: string) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  allowNegative?: boolean;
  size?: MantineSize;
  inputProps?: Omit<TextInputProps, "value" | "onChange" | "onBlur" | "label" | "required" | "disabled" | "readOnly" | "error" | "size" | "placeholder">;
}

const FieldText = (props: FieldTextProps) => {
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
  const { isReadOnly, canUpdate } = useFieldPermissions(field);
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
    const fieldType = field.type as CSNumericType & "String" & "Guid" & "Char";
    const isNullable = field.isNullable;
    const oldValue = value;
    let processedValue = inputValue;
    let shouldUpdate = false;

    const numberTypes = [
      "Single",
      "Double",
      "Decimal",
      "Byte",
      "SByte",
      "Int16",
      "UInt16",
      "Int32",
      "UInt32",
      "Int64",
      "UInt64",
    ];

    // Handle numeric types
    if (numberTypes.includes(fieldType)) {
      if (processedValue === "") {
        processedValue = "0";
      }

      if (processedValue === "-" && oldValue.length > 1) {
        processedValue = "0";
      }

      if (processedValue.includes("-")) {
        processedValue = "-" + processedValue.replace("-", "");
      }

      // Handle negative number allowance
      if (props.allowNegative === false && processedValue.startsWith("-")) {
        processedValue = processedValue.replace("-", "");
        if (processedValue === "") {
          processedValue = "0";
        }
      }

      // Handle number range
      if (field.numberRange != null) {
        const range = field.numberRange.split("-");
        const min = parseFloat(parseFloatCS(range[0], fieldType));
        const max = parseFloat(parseFloatCS(range[1], fieldType));
        try {
          const num = parseFloat(parseFloatCS(processedValue, fieldType));
          if (num <= min) {
            processedValue = min.toString();
          }
          if (num > max) {
            processedValue = max.toString();
          }
        } catch (e) {
          console.log(e);
        }
      }
    }

    // Handle Char type
    if (fieldType === "Char") {
      processedValue = processedValue.replace(oldValue, "");
      if (!isNullable && processedValue === "") {
        processedValue = "A";
      }
      shouldUpdate = true;
    }

    // Handle String and Guid types
    if (fieldType === "String" || fieldType === "Guid") {
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
    }

    // Handle float types
    if (["Single", "Double", "Decimal"].includes(fieldType)) {
      try {
        const number = parseFloatCS(processedValue, fieldType);
        if (!Number.isNaN(number) && number !== "NaN") {
          processedValue = number.toString();
          shouldUpdate = true;
        }
      } catch {}
    }

    // Handle integer types
    if (
      [
        "Byte",
        "SByte",
        "Int16",
        "UInt16",
        "Int32",
        "UInt32",
        "Int64",
        "UInt64",
      ].includes(fieldType)
    ) {
      try {
        const number = parseIntCS(processedValue, fieldType);
        if (!Number.isNaN(number)) {
          processedValue = number.toString();
          shouldUpdate = true;
        }
      } catch {}
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
    <TextInput
      label={label}
      size={props.size}
      ref={
        props.focus === true ? formContext.formBuilder.firstInputRef : undefined
      }
      value={value}
      onChange={(event) => handleChange(event.currentTarget.value)}
      onBlur={props.onBlur}
      readOnly={finalReadOnly}
      error={error}
      required={isRequired}
      placeholder={props.placeholder}
      disabled={props.disabled}
      {...props.inputProps}
    />
  );
};

export default FieldText;
