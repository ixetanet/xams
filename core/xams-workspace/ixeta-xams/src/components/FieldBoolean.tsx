import React from "react";
import { Checkbox, CheckboxProps, MantineSize } from "@mantine/core";
import { useFormContext } from "../contexts/FormContext";
import {
  useFieldValue,
  useFieldValidation,
  useFieldPermissions,
  useFieldLabel,
  useFieldRequired,
} from "../hooks/useFieldHelpers";

interface FieldBooleanProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  onChange?: (checked: boolean) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  size?: MantineSize;
  checkboxProps?: Omit<CheckboxProps, "checked" | "onChange" | "onBlur" | "label" | "required" | "disabled" | "readOnly" | "error" | "size">;
}

const FieldBoolean = (props: FieldBooleanProps) => {
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
    (formContext.formBuilder.snapshot !== undefined &&
      formContext.formBuilder.canUpdate === false);

  const isRequired = field.isRequired === true || props.required;

  const handleChange = (checked: boolean) => {
    if (!finalReadOnly) {
      setValue(checked);
      props.onChange?.(checked);
    }
  };

  if (value === undefined || value === null) {
    return null;
  }

  return (
    <div className="w-full h-full flex items-end">
      <div className="w-full h-9 flex items-center">
        <Checkbox
          ref={
            props.focus === true
              ? formContext.formBuilder.firstInputRef
              : undefined
          }
          label={label}
          size={props.size}
          checked={value}
          onChange={(event) => handleChange(event.currentTarget.checked)}
          onBlur={props.onBlur}
          readOnly={finalReadOnly}
          error={error}
          required={isRequired}
          disabled={props.disabled}
          {...props.checkboxProps}
        />
      </div>
    </div>
  );
};

export default FieldBoolean;
