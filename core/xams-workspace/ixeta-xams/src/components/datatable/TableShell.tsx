import {
  Button,
  Pagination,
  SegmentedControl,
  Table,
  useMantineTheme,
} from "@mantine/core";
import React, { useEffect, useState } from "react";
import { IconPlus, IconDotsVertical } from "@tabler/icons-react";
import { useDataTableContext } from "../DataTableImp";
import { getDataOptions } from "./DataTableTypes";
import SearchBox from "./SearchBox";
import { PermissionLevel } from "../../stores/usePermissionStore";
import DataTableHeader from "./DataTableHeader";
import DataTableOverlay from "./DataTableOverlay";
import DataTableOptions from "./DataTableOptions";

export interface TableOptions {
  highlighOnHover?: boolean;
  striped?: boolean;
  withBorder?: boolean;
  withColumnBorders?: boolean;
  truncate?: boolean;
}

interface TableShellProps {
  children?: any;
  searchFieldData?: { value: string; label: string }[];
}

const TableShell = (props: TableShellProps) => {
  const ctx = useDataTableContext();
  const theme = useMantineTheme();

  const isMetadataLoaded = ctx.state.metadata !== undefined;

  const scroll =
    ctx.props.scrollable === true || ctx.props.scrollable === undefined;

  const addEnabled =
    (ctx.props.canCreate === undefined || ctx.props.canCreate === true) &&
    ctx.state.permissions.create !== "NONE" &&
    (ctx.props.disabledMessage !== undefined ? false : true);

  const paginationEnabled =
    ctx.props.disabledMessage == null && ctx.state.permissions.read !== "NONE"
      ? true
      : false;

  const addButton =
    ctx.props.customCreateButton != null
      ? ctx.props.customCreateButton?.(() => ctx.openForm(undefined))
      : undefined;

  const searchEnabled =
    ctx.state.permissions.read !== "NONE" &&
    ctx.props.disabledMessage === undefined &&
    (ctx.props.searchable == null || ctx.props.searchable === true)
      ? true
      : false;

  const showActiveSwitch =
    ctx.props.showActiveSwitch === true &&
    ctx.state.permissions.read !== "NONE" &&
    ctx.props.disabledMessage === undefined
      ? true
      : false;

  const showOptions =
    ctx.props.showOptions === true || ctx.props.showOptions == null;

  const bodyOverlay =
    ctx.props.disabledMessage !== undefined ||
    ctx.state.permissions.read === "NONE" ||
    ctx.state.isLoadingData === true ? (
      <DataTableOverlay
        tableName={ctx.props.tableName}
        permissions={ctx.state.permissions}
        isLoadingData={ctx.state.isLoadingData}
        disabledMessage={ctx.props.disabledMessage}
      ></DataTableOverlay>
    ) : undefined;

  const showTopOptions =
    ctx.props.title === undefined ||
    ctx.props.title !== "" ||
    showOptions ||
    showActiveSwitch ||
    searchEnabled ||
    addEnabled ||
    addButton != null;

  const onChangePage = async (page: number) => {
    await ctx.getData({
      ...getDataOptions,
      page: page,
      orderBy: ctx.state.data.orderBy,
      setData: true,
    });
    if (ctx.props.onPageChange !== undefined) {
      ctx.props.onPageChange(page);
    }
  };

  const onActiveSwitchChange = async (active: string) => {
    const resp = await ctx.getData({
      ...getDataOptions,
      page: 1,
      active: active === "Active" ? true : false,
      setData: true,
      searchField: ctx.state.searchField as string,
      searchValue: ctx.state.searchValue as string,
      orderBy: ctx.state.data.orderBy,
    });

    ctx.dispatch({
      type: "ACTIVE_SWITCH_CHANGE",
      payload: {
        activeSwitch: active,
        data: resp.data,
      },
    });
  };

  return (
    <div className={`relative flex flex-col h-full w-full rtable`}>
      {bodyOverlay !== undefined && (
        <div className=" absolute w-full h-full pt-11">
          <div className="w-full h-full">{bodyOverlay}</div>
        </div>
      )}
      <div className="text-lg flex justify-between items-center">
        <div className="flex items-center">
          {!isMetadataLoaded
            ? ""
            : ctx.props.title !== undefined
            ? ctx.props.title
            : `Manage ${
                ctx.state.metadata === undefined
                  ? ctx.props.tableName
                  : ctx.state.metadata?.displayName
              }s`}
        </div>

        <div
          className={`${showTopOptions ? `flex` : `hidden`} items-center gap-2`}
        >
          {showOptions && <DataTableOptions></DataTableOptions>}

          {showActiveSwitch && (
            <div className=" m-2">
              <SegmentedControl
                data={["Active", "Inactive"]}
                size="sm"
                styles={{
                  label: {
                    paddingLeft: "0.5125rem",
                    paddingRight: "0.5125rem",
                    paddingTop: 0,
                    paddingBottom: 0,
                  },
                }}
                onChange={(value) => {
                  onActiveSwitchChange(value);
                }}
              ></SegmentedControl>
            </div>
          )}
          {searchEnabled === true ? (
            <SearchBox></SearchBox>
          ) : (
            <div className=" w-1 h-9"></div>
          )}

          {addEnabled && addButton == null && (
            <div
              onClick={() => {
                ctx.openForm(undefined);
                if (ctx.props.formOnOpen != null) {
                  ctx.props.formOnOpen("CREATE", undefined);
                }
              }}
              className="p-1 rounded-full w-8 h-8 flex justify-center items-center cursor-pointer"
              style={{
                backgroundColor: theme.fn.primaryColor(),
              }}
            >
              <IconPlus size={22} strokeWidth={2} color={"white"} />
            </div>
          )}
          {addEnabled && addButton != null && addButton}
        </div>
      </div>
      <Table
        aria-label={ctx.props.title}
        highlightOnHover={ctx.props.tableStyle?.highlighOnHover ?? true}
        striped={ctx.props.tableStyle?.striped ?? false}
        withBorder={ctx.props.tableStyle?.withBorder ?? false}
        withColumnBorders={ctx.props.tableStyle?.withColumnBorders ?? false}
        className={`${scroll ? `  overflow-x-hidden` : ``} flex flex-col grow`}
      >
        <thead className={`${scroll ? `px-2` : ``}`}>
          <tr className={`flex`}>
            <DataTableHeader />
          </tr>
        </thead>
        <tbody
          className={`relative overflow-x-hidden ${
            scroll ? `grow overflow-y-scroll pl-2 pr-0.5` : ``
          } `}
        >
          {props.children}
        </tbody>
      </Table>
      {paginationEnabled === true && (
        <div className="w-full flex justify-between items-center mt-4 px-2">
          <div className=" text-sm">
            {ctx.state.data.totalResults !== undefined &&
              ctx.state.data.maxResults &&
              ctx.state.data.currentPage && (
                <>
                  {ctx.state.data.results.length === 0
                    ? 0
                    : ctx.state.data.currentPage * ctx.state.data.maxResults -
                      (ctx.state.data.maxResults - 1)}
                  {` - `}
                  {ctx.state.data.currentPage * ctx.state.data.maxResults >
                  ctx.state.data.totalResults
                    ? ctx.state.data.totalResults
                    : ctx.state.data.currentPage * ctx.state.data.maxResults}
                  {` of `}
                  {ctx.state.data.totalResults}
                </>
              )}
          </div>
          {(ctx.props.pagination == null || ctx.props.pagination === true) && (
            <Pagination
              value={ctx.state.data.currentPage}
              total={ctx.state.data.pages < 1 ? 1 : ctx.state.data.pages}
              onChange={(page) => {
                onChangePage(page);
              }}
              size="sm"
              withEdges
            />
          )}
        </div>
      )}
    </div>
  );
};

export default TableShell;
