import React from "react";
import DataCell from "./DataCell";
import { useDataTableContext } from "../DataTableImp";
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
                  key={r[ctx.props.tableName + "Id"]}
                  className="w-full"
                >
                  {ctx.props.customRow(r)}
                </Table.Tr>
              );
            }
            return (
              <Table.Tr
                key={r[ctx.props.tableName + "Id"]}
                className="flex relative items-start"
                onClick={() => {
                  ctx.openForm(r);
                  if (ctx.props.formOnOpen != null) {
                    ctx.props.formOnOpen("UPDATE", r);
                  }
                }}
              >
                {ctx.getFields().map((f, i) => {
                  const key = `${r[ctx.props.tableName + "Id"]}-${i}`;
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
