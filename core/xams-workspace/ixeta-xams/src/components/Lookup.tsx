import { MetadataField } from "../api/MetadataResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import { Group, OptionsFilter, Select, SelectProps, Text } from "@mantine/core";
import { useDebouncedValue } from "@mantine/hooks";
import React, {
  Ref,
  forwardRef,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
} from "react";
import { LookupQuery } from "../reducers/formbuilderReducer";
import { ReadOrderBy } from "../api/ReadRequest";

type DataItem = {
  label: string;
  value: string;
  description?: string;
  data?: string;
};

interface LookupProps {
  label?: React.ReactNode;
  metaDataField: MetadataField;
  owningTableName?: string;
  defaultLabelValue?: DataItem;
  value?: string;
  onChange?: (
    value: {
      id: string | null;
      value: string | null;
      label: string | null;
    } | null
  ) => void;
  onBlur?: () => void;
  excludeValues?: string[];
  query?: LookupQuery | undefined;
  readOnly?: boolean;
  required?: boolean;
  error?: string;
  className?: string;
  disabled?: boolean;
  size?: string;
}

interface CustomSelectOption {
  value: string;
  label: string;
  description: string; // Add your additional prop
}

// Update the renderOption type to work with your custom option type
const renderSelectOption: SelectProps["renderOption"] = ({
  option,
  checked,
}) => {
  // Cast the option to your custom type
  const customOption = option as CustomSelectOption;

  return (
    <Group flex="1" gap="xs">
      <div>
        <Text size="sm">{customOption.label}</Text>
        <Text size="xs" opacity={0.65}>
          {customOption.description}
        </Text>
      </div>
    </Group>
  );
};

const Lookup = forwardRef((props: LookupProps, ref: Ref<HTMLInputElement>) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const authRequest = useAuthRequest();
  const [isSelectOpen, setIsSelectOpen] = useState(false);
  const [selectedItem, setSelectedItem] = useState<DataItem | null>(
    props.defaultLabelValue !== undefined ? props.defaultLabelValue : null
  );
  const [data, setData] = useState<DataItem[]>(
    props.defaultLabelValue !== undefined ? [props.defaultLabelValue] : []
  );
  const [searchValue, setSearchValue] = React.useState<string | null>(null);
  const [debouncedSearchValue] = useDebouncedValue(searchValue, 200, {
    leading: true,
  });
  useImperativeHandle(ref, () => inputRef.current as HTMLInputElement);

  const getData = async () => {
    let fields = [props.metaDataField.lookupPrimaryKeyField];
    if (props.metaDataField.lookupTableNameField != null) {
      fields.push(props.metaDataField.lookupTableNameField);
    }
    if (props.metaDataField.lookupTableDescriptionField != null) {
      fields.push(props.metaDataField.lookupTableDescriptionField);
    }
    // If this is an option field, we need to get the value as well
    if (props.metaDataField.option !== "") {
      fields.push("Value");
    }
    const readResp = await authRequest.read({
      tableName: props.metaDataField.lookupTable,
      maxResults: 20,
      page: 1,
      fields: fields,
      orderBy: [
        // If option, order by order field first
        ...(props.metaDataField.option !== ""
          ? ([{ field: "Order", order: "asc" }] as ReadOrderBy[])
          : []),
        {
          field:
            props.metaDataField.lookupTableNameField ??
            props.metaDataField.lookupPrimaryKeyField,
          order: props.query?.order,
        },
      ],
      filters: [
        {
          logicalOperator: "OR",
          filters: [
            // search on Name field
            {
              field:
                props.metaDataField.lookupTableNameField ??
                props.metaDataField.lookupPrimaryKeyField,
              operator: "contains",
              value:
                selectedItem != null &&
                selectedItem.label === debouncedSearchValue
                  ? ""
                  : debouncedSearchValue,
            },
            // search on description field
            ...(props.metaDataField.lookupTableDescriptionField != null
              ? [
                  {
                    field: props.metaDataField.lookupTableDescriptionField,
                    operator: "contains",
                    value:
                      selectedItem != null &&
                      selectedItem.label === debouncedSearchValue
                        ? ""
                        : debouncedSearchValue,
                  },
                ]
              : [{}]),
          ],
        },
        ...(props.query?.filters ?? []),
        ...(props.metaDataField.option !== ""
          ? [{ field: "Name", value: props.metaDataField.option }]
          : []),
      ],
      joins: props.query?.joins,
      except: props.query?.except,
      ...(props.metaDataField.name === "OwningUserId" ||
      props.metaDataField.name === "OwningTeamId"
        ? {
            parameters: {
              isOwnerField: true,
              tableName: props.owningTableName,
            },
          }
        : {}),
    });

    if (!readResp || !readResp.succeeded) return;
    let results = readResp.data.results.map((d: any) => ({
      // Append the description field to the value so it can be used in the search
      value: `${d[props.metaDataField.lookupPrimaryKeyField]}`,
      label: (
        d[props.metaDataField.lookupTableNameField] ??
        d[props.metaDataField.lookupPrimaryKeyField]
      ).toString(),

      // If there's a description field, add it to the label
      ...(props.metaDataField.lookupTableDescriptionField != null
        ? {
            description: `${
              d[props.metaDataField.lookupTableDescriptionField]
            }`,
          }
        : {}),
      ...(props.metaDataField.option !== ""
        ? {
            data: d["Value"],
          }
        : {}),
    })) as DataItem[];

    // If there are any values to exclude, remove them from the list
    if (props.excludeValues !== undefined) {
      results = results.filter(
        (x) => props.excludeValues?.find((y) => y === x.value) === undefined
      );
    }

    // If there's a default value, make sure it's in the list
    if (
      selectedItem != null &&
      results.find((x) => x.value === selectedItem?.value) === undefined
    ) {
      results.push(selectedItem);
    }

    results = results.map((x) => {
      if (x.label === undefined) {
        x.label = x.value;
      }
      return x;
    });

    setData(results);
  };

  // Override the default filter function to return all options
  // This allows for description fields to be used in search
  const optionsFilter: OptionsFilter = ({ options, search }) => {
    return options;
  };

  useEffect(() => {
    if (
      isSelectOpen &&
      debouncedSearchValue !== null &&
      !["CreatedById", "UpdatedById"].includes(props.metaDataField.name)
    ) {
      getData();
    }
  }, [debouncedSearchValue, isSelectOpen]);

  useEffect(() => {
    // If the default value changes and it isn't in the list, add it
    // This is necessary because the default value may change after the component is mounted
    if (
      props.defaultLabelValue !== undefined &&
      data.find((x) => x.value === props.defaultLabelValue?.value) === undefined
    ) {
      // don't add if if'ts ready in the list
      if (
        data.find(
          (x) =>
            x.value ===
            (typeof props.defaultLabelValue?.value === "number"
              ? `${props.defaultLabelValue?.value}`
              : props.defaultLabelValue?.value)
        )
      ) {
        return;
      }
      setData([...data, props.defaultLabelValue]);
    }
  }, [props.defaultLabelValue]);

  // Don't allow label to be null
  for (let item of data) {
    if (item.label == null) {
      item.label = item.value;
    }
    // Don't allow value to be number
    if (typeof item.value === "number") {
      item.value = `${item.value}`;
    }
  }

  return (
    <Select
      label={props.label}
      size={props.size}
      data={data}
      className={props.className}
      renderOption={
        props.metaDataField.lookupTableDescriptionField != null
          ? renderSelectOption
          : undefined
      }
      defaultValue={
        props.defaultLabelValue !== undefined
          ? props.defaultLabelValue.value
          : undefined
      }
      value={typeof props.value === "number" ? `${props.value}` : props.value}
      onSearchChange={setSearchValue}
      onChange={(value) => {
        if (props.onChange !== undefined) {
          props.onChange({
            id: value,
            value: data.find((x) => x.value === value)?.data ?? null,
            label: data.find((x) => x.value === value)?.label ?? null,
          });
        }
        setSelectedItem(data.find((x) => x.value === value) ?? null);
      }}
      onClick={() => {
        setIsSelectOpen(true);
      }}
      onBlur={props.onBlur}
      searchable
      clearable={props.metaDataField.isNullable}
      ref={inputRef}
      readOnly={props.readOnly}
      withAsterisk={props.required}
      error={props.error}
      disabled={props.disabled}
      filter={
        props.metaDataField.lookupTableDescriptionField != null
          ? optionsFilter
          : undefined
      }
    ></Select>
  );
});

Lookup.displayName = "XLookup";
export default Lookup;
