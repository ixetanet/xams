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
import MultiSelectComponent from "./MultiSelect";

export interface MultiSelectValue {
  id: string;
  name: string;
}

interface FieldMultiSelectProps {
  name: string;
  label?: string | React.ReactNode;
  onChange?: (values: Array<MultiSelectValue>) => void;
  onBlur?: () => void;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  size?: MantineSize;
}

const FieldMultiSelect = (props: FieldMultiSelectProps) => {
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

  const isRequired = field.isRequired === true || props.required === true;

  const handleChange = (values: Array<MultiSelectValue>) => {
    setValue(values);
    props.onChange?.(values);
  };

  if (value === undefined) {
    return null;
  }

  return (
    <MultiSelectComponent
      label={label}
      size={props.size}
      className={
        formContext.formBuilder.canRead.canRead === false ? "invisible" : ""
      }
      metaDataField={field}
      owningRecordId={
        formContext.formBuilder.metadata?.primaryKey
          ? formContext.formBuilder.data[
              formContext.formBuilder.metadata.primaryKey
            ]
          : undefined
      }
      value={value}
      onChange={handleChange}
      onBlur={props.onBlur}
      readOnly={finalReadOnly}
      required={isRequired}
      error={error}
      disabled={props.disabled}
    />
  );
};

export default FieldMultiSelect;
