import React from "react";
import {
  Checkbox,
  MantineSize,
  TextInput,
  Textarea,
  Tooltip,
} from "@mantine/core";
import { DateInput } from "@mantine/dates";
import { LookupQuery } from "../reducers/formbuilderReducer";
import { useFormContext } from "../contexts/FormContext";
import { DateInputProps } from "@mantine/dates";
import { MetadataField } from "../api/MetadataResponse";
import Lookup from "./Lookup";
import RichText from "./RichText";
import {
  CSNumericType,
  parseFloatCS,
  parseIntCS,
} from "../utils/CsNumberTypes";
// const RichText = React.lazy(() => import("./RichText"));
// const Lookup = React.lazy(() => import("./Lookup"));
// const DateInput = React.lazy(() =>
//   import("@mantine/dates").then((module) => ({ default: module.DateInput }))
// );

interface FieldProps {
  name: string;
  label?: string | React.ReactNode;
  focus?: boolean;
  varient?: "rich" | "textarea";
  placeholder?: string;
  dateInput?: DateInputProps;
  onChange?: (
    value: string | boolean | null | undefined,
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

  if (props.required) {
    formContext.formBuilder.addRequiredField(field.name);
  } else {
    formContext.formBuilder.removeRequiredField(field.name);
  }

  const isReadOnly = field.isReadOnly || props.readOnly;

  const setValue = (field: MetadataField, value: string) => {
    let updateValue = false;
    const fieldName = field.name;
    const fieldType = field.type as CSNumericType & "String" & "Guid" & "Char";
    const isNullable = field.isNullable;
    const oldValue = formContext.formBuilder.data[fieldName];
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

    if (numberTypes.includes(fieldType) && value === "") {
      value = "0";
    }

    if (
      numberTypes.includes(fieldType) &&
      value === "-" &&
      oldValue.length > 1
    ) {
      value = "0";
    }

    if (numberTypes.includes(fieldType) && value.includes("-")) {
      value = "-" + value.replace("-", "");
    }

    if (numberTypes.includes(fieldType)) {
      // If the field doesn't allow negative numbers, remove the negative sign
      if (props.allowNegative === false && value.startsWith("-")) {
        value = value.replace("-", "");
        if (value === "") {
          value = "0";
        }
      }
      // If there's a specific number range for the field, enforce it
      if (field.numberRange != null) {
        const range = field.numberRange.split("-");
        const min = parseFloat(parseFloatCS(range[0], fieldType));
        const max = parseFloat(parseFloatCS(range[1], fieldType));
        try {
          const num = parseFloat(parseFloatCS(value, fieldType));
          if (num <= min) {
            value = min.toString();
          }
          if (num > max) {
            value = max.toString();
          }
        } catch (e) {
          console.log(e);
        }
      }
    }

    if (fieldType == "Char") {
      value = value.replace(oldValue, "");
      if (!isNullable && value === "") {
        value = "A";
      }
      updateValue = true;
    }

    if (fieldType === "String" || fieldType === "Guid") {
      if (field.characterLimit) {
        if (value.length <= field.characterLimit) {
          updateValue = true;
        } else {
          value = value.substring(0, field.characterLimit);
          updateValue = true;
        }
      } else {
        updateValue = true;
      }
    }

    if (["Single", "Double", "Decimal"].includes(fieldType)) {
      try {
        const number = parseFloatCS(value, fieldType);
        if (!Number.isNaN(number) && number !== "NaN") {
          value = number.toString();
          updateValue = true;
        }
      } catch {}
    }

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
        const number = parseIntCS(value, fieldType);
        if (!Number.isNaN(number)) {
          value = number.toString();
          updateValue = true;
        }
      } catch {}
    }

    if (updateValue) {
      updateState(fieldName, value);
    }
  };

  const updateState = (fieldName: string, value: any, data: any = null) => {
    formContext.formBuilder.dispatch({
      type: "SET_FIELD_VALUE",
      payload: {
        field: fieldName,
        value: value,
      },
    });

    if (props.onChange != null) {
      // If field is a lookup return the id and value
      if (field?.type === "Lookup") {
        props.onChange(value, data);
      } else {
        props.onChange(value);
      }
    }
  };

  const removeTime = (dateFormat?: string) => {
    // If a time part is not included in the date format then remove it
    // Based on dayjs formats - https://day.js.org/docs/en/display/format
    if (dateFormat == null) {
      return true;
    }

    const timeParts = [
      "h",
      "m",
      "s",
      "A",
      "a",
      "H",
      "k",
      "K",
      "m",
      "s",
      "S",
      "Z",
      "X",
      "LT",
      "LTS",
      "LLL",
      "LLLL",
      "lll",
      "llll",
    ];
    if (timeParts.some((x) => dateFormat.includes(x))) {
      return false;
    }

    return true;
  };

  if (
    field == null ||
    formContext.formBuilder.data === undefined ||
    formContext.formBuilder.data[field.name] === undefined
  ) {
    return <></>;
  }

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

  const Label = ({ field }: { field: MetadataField }) => {
    return (
      <>
        {props.label != null ? props.label : field.displayName}{" "}
        {field.isRecommended === true ? (
          <Tooltip label="Recommended" withArrow>
            <span className=" text-blue-600">+</span>
          </Tooltip>
        ) : (
          <></>
        )}
      </>
    );
  };

  return (
    <>
      {field.type === "Lookup" && (
        <Lookup
          label={<Label field={field}></Label>}
          size={props.size}
          className={
            formContext.formBuilder.canRead.canRead === false ? "invisible" : ""
          }
          metaDataField={field}
          owningTableName={
            formContext.formBuilder.metadata?.tableName as string
          }
          ref={
            props.focus === true
              ? formContext.formBuilder.firstInputRef
              : undefined
          }
          onChange={(value) => {
            updateState(field.name, value?.id, value?.value);
            if (props.onChange != null) {
              props.onChange(value?.id, value?.label);
            }
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
          value={formContext.formBuilder.data[field.name]}
          excludeValues={
            formContext.formBuilder.lookupExclusions?.find(
              (x) => x.fieldName === field.name
            )?.values as string[]
          }
          query={
            formContext.formBuilder.lookupQueries?.find(
              (x) => x.field === field.name
            ) as LookupQuery
          }
          readOnly={
            ["CreatedById", "UpdatedById"].includes(field.name) ||
            (formContext.formBuilder.canUpdate === false &&
              formContext.formBuilder.snapshot !== undefined) ||
            (["OwningUserId", "OwningTeamId"].includes(field.name) &&
              formContext.formBuilder.snapshot !== undefined &&
              formContext.formBuilder.snapshot["_ui_info_"].canAssign ===
                false) ||
            isReadOnly
          }
          required={
            (field.isNullable === false &&
              !["CreatedById", "UpdatedById"].includes(field.name)) ||
            field.isRequired === true ||
            props.required === true
          }
          error={
            formContext.formBuilder.validationMessages.find(
              (x) => x.field === field.name
            )?.message
          }
          disabled={props.disabled}
        ></Lookup>
      )}
      {field.type === "String" && props.varient === "rich" && (
        <RichText
          value={formContext.formBuilder.data[field.name]}
          onChange={(value) => {
            setValue(field, value);
          }}
        ></RichText>
      )}
      {field.type === "String" && props.varient === "textarea" && (
        <Textarea
          size={props.size}
          styles={{
            root: {
              height: "100%",
              display: "flex",
              flexDirection: "column",
            },
            wrapper: {
              // height: "100%",
              flexGrow: 1,
            },
            input: {
              height: "100%",
            },
          }}
          label={<Label field={field} />}
          placeholder={props.placeholder}
          value={formContext.formBuilder.data[field.name]}
          required={
            field.isRequired === true ||
            props.required === true ||
            !field.isNullable
          }
          onChange={(event) => {
            setValue(field, event.currentTarget.value);
          }}
          onBlur={props.onBlur}
          readOnly={
            (formContext.formBuilder.canUpdate === false &&
              formContext.formBuilder.snapshot !== undefined) ||
            isReadOnly
          }
          error={
            formContext.formBuilder.validationMessages.find(
              (x) => x.field === field.name
            )?.message
          }
          disabled={props.disabled}
        />
      )}
      {(
        [
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
        ] as string[]
      ).includes(field.type) &&
        props.varient == null &&
        formContext.formBuilder.data[field.name] != null && (
          <TextInput
            label={<Label field={field} />}
            size={props.size}
            ref={
              props.focus === true
                ? formContext.formBuilder.firstInputRef
                : undefined
            }
            value={formContext.formBuilder.data[field.name]}
            onChange={(event) => {
              setValue(field, event.currentTarget.value);
            }}
            onBlur={props.onBlur}
            readOnly={
              (formContext.formBuilder.canUpdate === false &&
                formContext.formBuilder.snapshot !== undefined) ||
              isReadOnly
            }
            error={
              formContext.formBuilder.validationMessages.find(
                (x) => x.field === field.name
              )?.message
            }
            required={
              field.isRequired === true ||
              props.required === true ||
              !field.isNullable
            }
            placeholder={props.placeholder}
            disabled={props.disabled}
          ></TextInput>
        )}
      {field.type === "DateTime" && (
        <DateInput
          ref={
            props.focus === true
              ? formContext.formBuilder.firstInputRef
              : undefined
          }
          size={props.size}
          label={<Label field={field} />}
          // size={props.size}
          {...(field.dateFormat != null && field.dateFormat !== ""
            ? { valueFormat: field.dateFormat }
            : {})}
          value={
            formContext.formBuilder.data !== undefined &&
            formContext.formBuilder.data[field.name] !== undefined &&
            formContext.formBuilder.data[field.name] !== null &&
            formContext.formBuilder.data[field.name] !== "0001-01-01T00:00:00"
              ? removeTime(field.dateFormat)
                ? new Date(
                    formContext.formBuilder.data[field.name].replace("Z", "") // Prevent UTC conversion
                  )
                : new Date(formContext.formBuilder.data[field.name])
              : null
          }
          onChange={(event) => {
            let dateTime = event;
            updateState(
              field.name,
              event == null
                ? null
                : removeTime(field.dateFormat)
                ? (() => {
                    dateTime?.setUTCHours(0, 0, 0, 0);
                    return dateTime?.toISOString().replace("Z", "");
                  })()
                : dateTime?.toISOString()
            );
          }}
          onBlur={props.onBlur}
          clearable={field.isNullable}
          readOnly={
            ["CreatedDate", "UpdatedDate"].includes(field.name) ||
            (formContext.formBuilder.snapshot !== undefined &&
              formContext.formBuilder.canUpdate === false) ||
            isReadOnly
          }
          required={
            field.isRequired === true || props.required || !field.isNullable
          }
          error={
            formContext.formBuilder.validationMessages.find(
              (x) => x.field === field.name
            )?.message
          }
          {...props.dateInput}
          disabled={props.disabled}
        ></DateInput>
      )}
      {field.type === "Boolean" && (
        <div className="w-full h-full flex items-end">
          <div className="w-full h-9 flex items-center">
            <Checkbox
              ref={
                props.focus === true
                  ? formContext.formBuilder.firstInputRef
                  : undefined
              }
              label={<Label field={field} />}
              size={props.size}
              checked={
                formContext.formBuilder.data !== undefined &&
                formContext.formBuilder.data[field.name] !== undefined &&
                formContext.formBuilder.data[field.name] !== null
                  ? formContext.formBuilder.data[field.name]
                  : undefined
              }
              onChange={(event) => {
                const readOnly =
                  (formContext.formBuilder.snapshot !== undefined &&
                    formContext.formBuilder.canUpdate === false) ||
                  isReadOnly;
                if (!readOnly) {
                  updateState(field.name, event.currentTarget.checked);
                }
                if (props.onChange != null) {
                  props.onChange(event.currentTarget.checked);
                }
              }}
              onBlur={props.onBlur}
              readOnly={
                (formContext.formBuilder.snapshot !== undefined &&
                  formContext.formBuilder.canUpdate === false) ||
                isReadOnly
              }
              error={
                formContext.formBuilder.validationMessages.find(
                  (x) => x.field === field.name
                )?.message
              }
              required={field.isRequired === true || props.required}
              disabled={props.disabled}
            ></Checkbox>
          </div>
        </div>
      )}
    </>
  );
};

export default Field;
