import { Select, TextInput } from "@mantine/core";
import { useDebouncedValue } from "@mantine/hooks";
import React, { useEffect, useState } from "react";
import { useDataTableContext } from "./DataTableContext";
import { getDataOptions } from "./DataTableTypes";
import { getLocalTimeOffsetFromUTC } from "../../utils/Util";

const SearchBox = () => {
  const ctx = useDataTableContext();
  const [state, setState] = useState<{
    searchField: string | null;
    searchValue: string | null;
  }>({
    searchField: null,
    searchValue: null,
  });
  const [debouncedSearchValue] = useDebouncedValue(state.searchValue, 500);

  const getSearchFieldData = () => {
    return ctx
      .getFields()
      .filter(
        (f) => typeof f.displayName === "string" && f.metadataField != null
      )
      .map((f, i) => {
        const fieldName = f.metadataField?.lookupName ?? f.metadataField?.name;
        const withAlias = `${f.alias}${f.alias !== "" ? `.` : ``}${fieldName}`;
        return {
          value: withAlias,
          label: f.metadataField?.displayName as string,
        };
      });
  };

  const onSearchChange = async () => {
    // if search field a DateTime field
    const fieldType = ctx.state.metadata?.fields.find(
      (f) => f.name === state.searchField
    )?.type;
    let searchAppend = "";
    if (fieldType === "DateTime" && debouncedSearchValue?.trim() !== "") {
      const utcOffset = getLocalTimeOffsetFromUTC();
      searchAppend = `~${utcOffset}`;
    }

    const searchValue = debouncedSearchValue + searchAppend;

    const resp = await ctx.getData({
      ...getDataOptions,
      page: 1,
      searchField: state.searchField,
      searchValue: searchValue,
      active: ctx.state.activeSwitch === "Active",
      orderBy: ctx.state.data.orderBy,
    });
    ctx.dispatch({
      type: "SEARCH_VALUE_CHANGE",
      payload: {
        searchValue: searchValue,
        searchField: state.searchField,
        data: resp.data,
      },
    });
  };

  useEffect(() => {
    if (debouncedSearchValue != null) {
      onSearchChange();
    }
  }, [debouncedSearchValue, state.searchField]);

  useEffect(() => {
    if (ctx.props.tableName != null && ctx.props.tableName !== "") {
      setState({
        searchField: getSearchFieldData()?.[0]?.value ?? null,
        searchValue: null,
      });
    }
  }, [ctx.props.tableName]);

  return (
    <div className={`flex`}>
      <Select
        placeholder="Search"
        style={{
          width: "auto",
        }}
        data={getSearchFieldData() ?? []}
        value={state.searchField ?? ""}
        onChange={(value) => {
          setState({
            ...state,
            searchField: value,
          });
        }}
      />
      <TextInput
        placeholder="Search..."
        style={{
          width: "auto",
        }}
        value={state.searchValue ?? ""}
        onChange={(event) => {
          setState({
            ...state,
            searchValue: event.currentTarget.value,
          });
        }}
      />
    </div>
  );
};

export default SearchBox;
