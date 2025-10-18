import { MetadataField } from "../api/MetadataResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import {
  Group,
  MultiSelect as MantineMultiSelect,
  MultiSelectProps,
  OptionsFilter,
  Text,
} from "@mantine/core";
import { useDebouncedValue } from "@mantine/hooks";
import React, {
  Ref,
  forwardRef,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import { ReadRequest } from "../api/ReadRequest";

type DataItem = {
  label: string;
  value: string;
  description?: string;
};

interface MultiSelectComponentProps {
  label?: React.ReactNode;
  metaDataField: MetadataField;
  owningRecordId?: string; // The ID of the parent record (e.g., EmailMessageId)
  value?: Array<{ id: string; name: string }>; // Array of {id, name} objects from backend
  onChange?: (value: Array<{ id: string; name: string }>) => void;
  onBlur?: () => void;
  readOnly?: boolean;
  required?: boolean;
  error?: string;
  className?: string;
  disabled?: boolean;
  size?: string;
}

interface CustomMultiSelectOption {
  value: string;
  label: string;
  description: string;
}

// Custom render option showing name + description
const renderMultiSelectOption: MultiSelectProps["renderOption"] = ({
  option,
  checked,
}) => {
  const customOption = option as CustomMultiSelectOption;

  return (
    <Group flex="1" gap="xs">
      <div>
        <Text size="sm">{customOption.label}</Text>
        {customOption.description && (
          <Text size="xs" opacity={0.65}>
            {customOption.description}
          </Text>
        )}
      </div>
    </Group>
  );
};

const MultiSelectComponent = forwardRef(
  (props: MultiSelectComponentProps, ref: Ref<HTMLInputElement>) => {
    const inputRef = useRef<HTMLInputElement>(null);
    const authRequest = useAuthRequest();
    const [searchResults, setSearchResults] = useState<DataItem[]>([]);
    const [searchValue, setSearchValue] = React.useState<string | null>(null);
    const [debouncedSearchValue] = useDebouncedValue(searchValue, 200, {
      leading: true,
    });
    const [hasPermission, setHasPermission] = useState<boolean>(false);
    const [hasOpenedOnce, setHasOpenedOnce] = useState(false);

    useImperativeHandle(ref, () => inputRef.current as HTMLInputElement);

    const multiSelect = props.metaDataField.multiSelect;

    if (!multiSelect) {
      console.error("MultiSelect field is missing multiSelect configuration");
      return null;
    }

    const getPermissions = async () => {
      const readPermission = authRequest.hasAnyPermissions([
        `TABLE_${multiSelect.targetTable}_READ_USER`,
        `TABLE_${multiSelect.targetTable}_READ_TEAM`,
        `TABLE_${multiSelect.targetTable}_READ_SYSTEM`,
      ]);
      const createPermission = authRequest.hasAnyPermissions([
        `TABLE_${multiSelect.junctionTable}_CREATE_USER`,
        `TABLE_${multiSelect.junctionTable}_CREATE_TEAM`,
        `TABLE_${multiSelect.junctionTable}_CREATE_SYSTEM`,
      ]);
      const deletePermission = authRequest.hasAnyPermissions([
        `TABLE_${multiSelect.junctionTable}_DELETE_USER`,
        `TABLE_${multiSelect.junctionTable}_DELETE_TEAM`,
        `TABLE_${multiSelect.junctionTable}_DELETE_SYSTEM`,
      ]);
      // await all permission checks
      const permissionResp = await Promise.all([
        readPermission,
        createPermission,
        deletePermission,
      ]);
      // check that ALL permissions are true
      const permissionRespResult = permissionResp.every((p) => p === true);
      setHasPermission(permissionRespResult);
    };

    // Search target entities for MultiSelect dropdown
    const getData = async () => {
      if (!hasPermission) {
        return;
      }
      let fields = [multiSelect.targetPrimaryKeyField];
      if (multiSelect.targetNameField) {
        fields.push(multiSelect.targetNameField);
      }
      if (multiSelect.targetDescriptionField) {
        fields.push(multiSelect.targetDescriptionField);
      }

      const readRequest = {
        tableName: multiSelect.targetTable,
        maxResults: 20,
        page: 1,
        fields: fields,
        orderBy: [
          {
            field:
              multiSelect.targetNameField ?? multiSelect.targetPrimaryKeyField,
            order: "asc",
          },
        ],
        filters: [
          debouncedSearchValue
            ? {
                logicalOperator: "OR",
                filters: [
                  // search on Name field
                  {
                    field:
                      multiSelect.targetNameField ??
                      multiSelect.targetPrimaryKeyField,
                    operator: "contains",
                    value: debouncedSearchValue,
                  },
                  // search on description field
                  ...(multiSelect.targetDescriptionField
                    ? [
                        {
                          field: multiSelect.targetDescriptionField,
                          operator: "contains",
                          value: debouncedSearchValue,
                        },
                      ]
                    : []),
                ],
              }
            : {},
          ...(multiSelect.targetHasActiveField
            ? [
                {
                  field: "IsActive",
                  operator: "==",
                  value: "true",
                },
              ]
            : []),
        ],
      } as ReadRequest;

      const readResp = await authRequest.read(readRequest);

      if (!readResp || !readResp.succeeded) return;

      let results = readResp.data.results.map((d: any) => ({
        value: `${d[multiSelect.targetPrimaryKeyField]}`,
        label: (
          d[multiSelect.targetNameField] ?? d[multiSelect.targetPrimaryKeyField]
        ).toString(),
        ...(multiSelect.targetDescriptionField
          ? {
              description: `${d[multiSelect.targetDescriptionField] ?? ""}`,
            }
          : {}),
      })) as DataItem[];

      results = results.map((x) => {
        if (x.label === undefined) {
          x.label = x.value;
        }
        return x;
      });

      setSearchResults(results);
    };

    // Derive selected values from props instead of maintaining separate state
    const selectedValues = useMemo(() => {
      if (props.value && Array.isArray(props.value) && props.value.length > 0) {
        return props.value.map((item) => item.id);
      }
      return [];
    }, [props.value]);

    // Convert selected items from props into DataItem format
    const selectedItemsAsData = useMemo(() => {
      if (props.value && Array.isArray(props.value) && props.value.length > 0) {
        return props.value.map((item) => ({
          value: item.id,
          label: item.name,
        }));
      }
      return [];
    }, [props.value]);

    // Merge search results with selected items, removing duplicates
    const mergedData = useMemo(() => {
      if (selectedItemsAsData.length === 0) {
        return searchResults;
      }

      const selectedIds = new Set(selectedItemsAsData.map((item) => item.value));
      const uniqueSearchResults = searchResults.filter(
        (item) => !selectedIds.has(item.value)
      );

      const merged = [...selectedItemsAsData, ...uniqueSearchResults];
      merged.sort((a, b) => a.label.localeCompare(b.label));
      return merged;
    }, [searchResults, selectedItemsAsData]);

    // Handle MultiSelect value changes - convert IDs to {id, name} objects
    const handleChange = (newValues: string[]) => {
      if (props.onChange) {
        // Look up names from merged data and build array of {id, name} objects
        const valuesWithNames = newValues.map((id) => {
          const item = mergedData.find((d) => d.value === id);
          return {
            id: id,
            name: item?.label ?? id,
          };
        });

        props.onChange(valuesWithNames as any);
      }
    };

    // Override the default filter function to return all options
    // This allows for description fields to be used in search
    const optionsFilter: OptionsFilter = ({ options, search }) => {
      return options;
    };

    // Check permissions on mount
    useEffect(() => {
      getPermissions();
    }, []);

    // Fetch data when dropdown opens or search value changes
    useEffect(() => {
      if (hasOpenedOnce && hasPermission) {
        getData();
      }
    }, [debouncedSearchValue, hasOpenedOnce, hasPermission]);

    return (
      <MantineMultiSelect
        label={props.label}
        size={props.size}
        data={mergedData}
        className={props.className}
        renderOption={
          multiSelect.targetDescriptionField
            ? renderMultiSelectOption
            : undefined
        }
        value={selectedValues}
        onSearchChange={setSearchValue}
        onChange={handleChange}
        onClick={() => {
          if (!hasOpenedOnce) {
            setHasOpenedOnce(true);
          }
        }}
        onBlur={props.onBlur}
        searchable
        clearable
        ref={inputRef}
        readOnly={props.readOnly || !hasPermission}
        withAsterisk={props.required}
        error={props.error}
        disabled={props.disabled}
        filter={multiSelect.targetDescriptionField ? optionsFilter : undefined}
      />
    );
  }
);

MultiSelectComponent.displayName = "XMultiSelect";
export default MultiSelectComponent;
