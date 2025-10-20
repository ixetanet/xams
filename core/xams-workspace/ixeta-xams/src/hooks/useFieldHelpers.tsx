import React from "react";
import { Tooltip } from "@mantine/core";
import { useFormContext } from "../contexts/FormContext";
import { MetadataField } from "../api/MetadataResponse";

/**
 * Hook to get and set field value
 */
export const useFieldValue = (fieldName: string) => {
  const formContext = useFormContext();
  const value = formContext.formBuilder.data[fieldName];

  const setValue = (newValue: any) => {
    formContext.formBuilder.dispatch({
      type: "SET_FIELD_VALUE",
      payload: {
        field: fieldName,
        value: newValue,
      },
    });
  };

  return [value, setValue] as const;
};

/**
 * Hook to get validation error message for a field
 */
export const useFieldValidation = (fieldName: string) => {
  const formContext = useFormContext();
  const validationMessage = formContext.formBuilder.validationMessages.find(
    (x) => x.field === fieldName
  );

  return validationMessage?.message;
};

/**
 * Hook to determine field permissions (readOnly, required, etc.)
 */
export const useFieldPermissions = (field: MetadataField) => {
  const formContext = useFormContext();

  const isReadOnly =
    field.isReadOnly ||
    (field.isCreateOnly && formContext.formBuilder.operation === "UPDATE");

  const canUpdate =
    formContext.formBuilder.canUpdate !== false ||
    formContext.formBuilder.snapshot === undefined;

  return {
    isReadOnly,
    canUpdate,
    canRead: formContext.formBuilder.canRead.canRead !== false,
  };
};

/**
 * Hook to generate field label with recommended indicator
 */
export const useFieldLabel = (
  field: MetadataField,
  customLabel?: string | React.ReactNode
): React.ReactNode => {
  const label = customLabel != null ? customLabel : field.displayName;

  return (
    <>
      {label}{" "}
      {field.isRecommended === true ? (
        <Tooltip label="Recommended" withArrow>
          <span className=" text-blue-600">+</span>
        </Tooltip>
      ) : null}
    </>
  );
};

/**
 * Hook to add/remove required fields from form builder
 * Called during render - no useEffect needed since operations are synchronous and idempotent
 */
export const useFieldRequired = (fieldName: string, required?: boolean) => {
  const formContext = useFormContext();

  if (required) {
    formContext.formBuilder.addRequiredField(fieldName);
  } else {
    formContext.formBuilder.removeRequiredField(fieldName);
  }
};
