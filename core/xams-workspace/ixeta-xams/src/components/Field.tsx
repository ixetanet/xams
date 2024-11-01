import React from "react";
import { Checkbox, TextInput, Textarea, Tooltip } from "@mantine/core";
import { DateInput } from "@mantine/dates";
// import Lookup from "./Lookup";
import useLookupStore, { LookupStoreInfo } from "../stores/useLookupStore";
import { LookupQuery } from "../reducers/formbuilderReducer";
import { useFormContext } from "../contexts/FormContext";
import { DateInputProps } from "@mantine/dates";
import { MetadataField } from "../api/MetadataResponse";
const RichText = React.lazy(() => import("./RichText"));
const Lookup = React.lazy(() => import("./Lookup"));
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
}

const Field = (props: FieldProps) => {
  const formContext = useFormContext();
  const field = formContext.formBuilder.metadata?.fields.find(
    (x) => x.name === props.name
  );
  const lookupStore = useLookupStore();

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
    const fieldType = field.type;
    const isNullable = field.isNullable;
    const numberTypes = ["Single", "Int32", "Int64", "Double", "Decimal"];

    if (numberTypes.includes(fieldType)) {
      // If the user presses "-" when there's already a value, make it negative
      if (value.endsWith("-") && !value.startsWith("-")) {
        value = "-" + value.replace("-", "");
      }
      // If there's a leading zero, remove it
      if (value.startsWith("-0") || value.startsWith("0")) {
        try {
          if (fieldType.startsWith("Int") && parseInt(value) !== 0) {
            value = parseInt(value).toString();
          } else if (!fieldType.startsWith("Int") && parseFloat(value) !== 0) {
            value = parseFloat(value).toString();
          }
        } catch {}
      }
    }

    if (!isNullable && numberTypes.includes(fieldType)) {
      if (value === "-") {
        updateValue = true;
      }
      if (value === "0-") {
        value = "-0";
      }
      if (value === "-00") {
        value = "-0";
      }
      if (value === ".") {
        updateValue = true;
      }
      const pattern = /^-0[1-9]$/;
      if (pattern.test(value)) {
        value = value.replace("0", "");
      }
    } else if (
      isNullable &&
      (value === "-" || value === ".") &&
      numberTypes.includes(fieldType)
    ) {
      updateValue = true;
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
        const min = parseFloat(range[0]);
        const max = parseFloat(range[1]);
        try {
          const num = parseFloat(value);
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
    if (fieldType === "Single") {
      value = value
        .replace("Infinity", "")
        .replace("-Infinity", "")
        .replace("NaN", "");
      const floatPattern = /^-?(\d+(\.\d*)?|\.\d+)$/;
      if (floatPattern.test(value)) {
        updateValue = true;
      }
    }
    if (fieldType === "Int32") {
      const intPattern =
        /^-?(?:[0-9]{1,10}|[1-9][0-9]{0,8}|[12][0-9]{9}|20[0-9]{8}|21[0-4][0-9]{7}|214[0-6][0-9]{6}|2147[0-3][0-9]{5}|214748[0-2][0-9]{4}|2147483[0-5][0-9]{3}|21474836[0-3][0-9]{2}|214748364[0-7])$/;

      if (intPattern.test(value)) {
        updateValue = true;
      }
    }
    if (fieldType === "Int64") {
      const intPattern =
        /^-?(?:[0-9]{1,19}|[1-8][0-9]{0,18}|[9][0-1][0-9]{0,17}|92[0-1][0-9]{0,16}|922[0-2][0-9]{0,15}|9223[0-2][0-9]{0,14}|92233[0-6][0-9]{0,13}|922337[0-1][0-9]{0,12}|92233720[0-2][0-9]{0,10}|922337203[0-5][0-9]{0,9}|9223372036[0-4][0-9]{0,8}|92233720367[0-4][0-9]{0,7}|922337203685[0-3][0-9]{0,6}|9223372036854[0-6][0-9]{0,5}|92233720368547[0-4][0-9]{0,4}|922337203685477[0-4][0-9]{0,3}|9223372036854775[0-7][0-9]{0,2}|922337203685477580[0-7])$/;
      if (intPattern.test(value)) {
        updateValue = true;
      }
    }
    if (fieldType === "Double") {
      value = value
        .replace("Infinity", "")
        .replace("-Infinity", "")
        .replace("NaN", "");
      const doublePattern = /^-?(\d+(\.\d*)?|\.\d+)$/;
      if (doublePattern.test(value)) {
        updateValue = true;
      }
    }
    if (fieldType === "Decimal") {
      const decimalPattern = /^-?(\d+(\.\d*)?|\.\d+)$/;
      if (decimalPattern.test(value)) {
        updateValue = true;
      }
    }

    if (["Single", "Int32", "Int64", "Double", "Decimal"].includes(fieldType)) {
      if (isNullable && value === "-") {
        updateValue = true;
      }

      if (isNullable === true && value === "") {
        updateState(fieldName, "");
      } else if (isNullable === false && value === "") {
        updateState(fieldName, "0");
      } else if (updateValue) {
        updateState(fieldName, value);
      }
    } else if (updateValue) {
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
    fieldName: string,
    lookupTable: string
  ): LookupStoreInfo | undefined => {
    const fieldDefault = formContext.formBuilder.defaults?.find(
      (x) => x.field === fieldName
    );
    if (fieldDefault !== undefined) {
      return lookupStore.lookups.find(
        (x) => x.fieldName === fieldName && x.lookupTable === lookupTable
      );
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
              : getDefaultLookupItem(field.name, field.lookupTable)
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
        ] as string[]
      ).includes(field.type) &&
        props.varient == null &&
        formContext.formBuilder.data[field.name] != null && (
          <TextInput
            label={<Label field={field} />}
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
          label={<Label field={field} />}
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
