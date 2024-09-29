import React from "react";
import DataCell from "./DataCell";
import { useDataTableContext } from "../DataTableImp";

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
                <tr key={r[ctx.props.tableName + "Id"]} className="w-full">
                  {ctx.props.customRow(r)}
                </tr>
              );
            }
            return (
              <tr
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
              </tr>
            );
          })}
    </>
  );
};

export default DataRows;
