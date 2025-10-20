import React from "react";
import { useFormContext } from "../contexts/FormContext";
import RichText from "./RichText";

interface FieldRichTextProps {
  name: string;
  label?: string | React.ReactNode;
  onChange?: (value: string) => void;
}

const FieldRichText = (props: FieldRichTextProps) => {
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

  // Validate field type
  if (field.type !== "String") {
    throw Error(
      `FieldRichText can only be used with String fields. Field ${props.name} is type ${field.type}.`
    );
  }

  const setValue = (value: string) => {
    let finalValue = value;

    // Handle character limit
    if (field.characterLimit && value.length > field.characterLimit) {
      finalValue = value.substring(0, field.characterLimit);
    }

    updateState(finalValue);
  };

  const updateState = (value: string) => {
    formContext.formBuilder.dispatch({
      type: "SET_FIELD_VALUE",
      payload: {
        field: field.name,
        value: value,
      },
    });

    if (props.onChange != null) {
      props.onChange(value);
    }
  };

  if (
    formContext.formBuilder.data === undefined ||
    formContext.formBuilder.data[field.name] === undefined
  ) {
    return <></>;
  }

  return (
    <RichText
      value={formContext.formBuilder.data[field.name]}
      onChange={(value) => {
        setValue(value);
      }}
    />
  );
};

export default FieldRichText;
