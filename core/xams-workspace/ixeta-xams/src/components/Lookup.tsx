import { API_DATA_READ } from "../apiurls";
import { MetadataField } from "../api/MetadataResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import { Avatar, Group, Select, SelectProps, Text } from "@mantine/core";
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
}

interface ItemProps extends React.ComponentPropsWithoutRef<"div"> {
  image: string;
  label: string;
  description: string;
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

const SelectItem = forwardRef<HTMLDivElement, ItemProps>(
  ({ image, label, description, ...others }: ItemProps, ref) => (
    <div ref={ref} {...others}>
      <Group>
        <div>
          <Text size="sm">{label}</Text>
          <Text size="xs" opacity={0.65}>
            {description}
          </Text>
        </div>
      </Group>
    </div>
  )
);

const Lookup = forwardRef((props: LookupProps, ref: Ref<HTMLInputElement>) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const authRequest = useAuthRequest();
  const [selectedItem, setSelectedItem] = useState<DataItem | null>(
    props.defaultLabelValue !== undefined ? props.defaultLabelValue : null
  );
  const [data, setData] = useState<DataItem[]>(
    props.defaultLabelValue !== undefined ? [props.defaultLabelValue] : []
  );
  const [searchValue, setSearchValue] = React.useState<string | null>(null);
  const [debouncedSearchValue] = useDebouncedValue(searchValue, 300, {
    leading: true,
  });
  useImperativeHandle(ref, () => inputRef.current as HTMLInputElement);

  const getData = async () => {
    let fields = [
      `${props.metaDataField.lookupTable}Id`,
      props.metaDataField.lookupTableNameField,
    ];
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
          field: props.metaDataField.lookupTableNameField,
          order: props.query?.order,
        },
      ],
      filters: [
        {
          field: props.metaDataField.lookupTableNameField,
          value:
            selectedItem != null && selectedItem.label === debouncedSearchValue
              ? ""
              : debouncedSearchValue,
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
      value: `${d[`${props.metaDataField.lookupTable}Id`]}`,
      label: d[props.metaDataField.lookupTableNameField],
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

  useEffect(() => {
    if (
      debouncedSearchValue !== null &&
      !["CreatedById", "UpdatedById"].includes(props.metaDataField.name)
    ) {
      getData();
    }
  }, [debouncedSearchValue]);

  useEffect(() => {
    // If the default value changes and it isn't in the list, add it
    // This is necessary because the default value may change after the component is mounted
    if (
      props.defaultLabelValue !== undefined &&
      data.find((x) => x.value === props.defaultLabelValue?.value) === undefined
    ) {
      setData([...data, props.defaultLabelValue]);
    }
  }, [props.defaultLabelValue]);

  // Don't allow label to be null
  for (let item of data) {
    if (item.label == null) {
      item.label = item.value;
    }
  }

  return (
    <Select
      label={props.label}
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
      value={props.value}
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
      onBlur={props.onBlur}
      searchable
      clearable={props.metaDataField.isNullable}
      ref={inputRef}
      readOnly={props.readOnly}
      withAsterisk={props.required}
      error={props.error}
      disabled={props.disabled}
    ></Select>
  );
});

Lookup.displayName = "XLookup";
export default Lookup;
