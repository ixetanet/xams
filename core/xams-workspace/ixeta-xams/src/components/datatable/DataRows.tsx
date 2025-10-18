import React from "react";
import DataCell from "./DataCell";
import { useDataTableContext } from "./DataTableContext";
import { Table } from "@mantine/core";

const DataRows = () => {
  const ctx = useDataTableContext();
  return (
    <>
      {ctx.state.isLoadingData === false &&
        ctx.state.data?.results != null &&
        ctx.state.data.results
          .filter((r) => ctx.props.tableName === ctx.state.metadata?.tableName)
          .map((r, j) => {
            if (ctx.props.customRow !== undefined) {
              return (
                <Table.Tr
                  key={
                    ctx.state.metadata ? r[ctx.state.metadata.primaryKey] : j
                  }
                  className="w-full"
                >
                  {ctx.props.customRow(r)}
                </Table.Tr>
              );
            }
            return (
              <Table.Tr
                key={ctx.state.metadata ? r[ctx.state.metadata.primaryKey] : j}
                className="flex relative items-start"
                onClick={() => {
                  ctx.openForm(r);
                  if (ctx.props.formOnOpen != null) {
                    ctx.props.formOnOpen("UPDATE", r);
                  }
                }}
              >
                {ctx.getFields().map((f, i) => {
                  const key = `${
                    ctx.state.metadata ? r[ctx.state.metadata.primaryKey] : j
                  }-${i}`;
                  return (
                    <DataCell key={key} record={r} fieldInfo={f}></DataCell>
                  );
                })}
              </Table.Tr>
            );
          })}
    </>
  );
};

export default DataRows;
