import { Select, TextInput } from "@mantine/core";
import { useDebouncedValue } from "@mantine/hooks";
import React, { useEffect, useState } from "react";
import { useDataTableContext } from "../DataTableImp";
import { getDataOptions } from "./DataTableTypes";

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
    const resp = await ctx.getData({
      ...getDataOptions,
      page: 1,
      searchField: state.searchField,
      searchValue: debouncedSearchValue,
      active: ctx.state.activeSwitch === "Active",
      orderBy: ctx.state.data.orderBy,
    });
    ctx.dispatch({
      type: "SEARCH_VALUE_CHANGE",
      payload: {
        searchValue: debouncedSearchValue,
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
