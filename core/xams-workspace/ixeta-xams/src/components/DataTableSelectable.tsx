import { Checkbox } from "@mantine/core";
import React, { useEffect, useRef } from "react";
import {
  DataTableField,
  DataTableProps,
  DataTableRef,
  SelectedRow,
} from "./datatable/DataTableTypes";
import DataTable from "./DataTable";

const DataTableSelectable = (
  props: DataTableProps & {
    varient: "single" | "multiple";
    fields: string[]; // Make fields and columnWidths required
    columnWidths: string[];
    onSelectedRowsChange?: (rows: SelectedRow[]) => void;
  }
) => {
  const [selectedRows, setSelectedRows] = React.useState<SelectedRow[]>([]);
  const [selectAll, setSelectAll] = React.useState<boolean>(false);
  const [tableData, setTableData] = React.useState<any[]>([]); // [{}
  const dataTableRef = useRef<DataTableRef>(null);

  const onSelectAll = (selected: boolean) => {
    setSelectAll(selected);
    if (tableData.length === 0) {
      return;
    }
    setSelectedRows((prev) =>
      selected
        ? [
            ...prev,
            ...tableData.map((r) => {
              return { id: r[`${props.tableName}Id`], row: r };
            }),
          ]
        : [
            ...prev.filter((r) => {
              return !tableData.some(
                (r2) => r2[`${props.tableName}Id`] === r.id
              );
            }),
          ]
    );
  };

  const onSelect = (selected: boolean, id: string, row: any) => {
    if (props.varient === "single") {
      setSelectedRows(
        selected
          ? [{ id, row }]
          : selectedRows.filter((r) => {
              return r.id !== id;
            })
      );
      return;
    }

    if (!selected && selectAll) {
      setSelectAll(false);
    }
    setSelectedRows((prev) =>
      selected
        ? [...prev, { id, row }]
        : prev.filter((r) => {
            return r.id !== id;
          })
    );

    // If all rows selected then select all checkbox
    const rowsToCheck = selected
      ? [...selectedRows, { id, row }]
      : selectedRows.filter((r) => {
          return r.id !== id;
        });
    for (let x of tableData) {
      if (
        rowsToCheck.find((r) => {
          return r.id === x[`${props.tableName}Id`];
        }) == null
      ) {
        setSelectAll(false);
        return;
      }
    }
    setSelectAll(true);
  };

  // Handle page change
  useEffect(() => {
    setSelectAll(
      tableData.every((r) => {
        return selectedRows.some((r2) => {
          return r2.id === r[`${props.tableName}Id`];
        });
      })
    );
  }, [tableData]);

  useEffect(() => {
    if (props.onSelectedRowsChange != null) {
      props.onSelectedRowsChange(selectedRows);
    }
  }, [selectedRows]);

  const selectableField = {
    // header: "Select",
    header: () => {
      if (props.varient === "single") {
        return <></>;
      }
      return (
        <Checkbox
          onChange={(event) => onSelectAll(event.target.checked)}
          checked={selectAll}
        ></Checkbox>
      );
    },
    body: (record) => {
      return (
        <Checkbox
          onChange={(event) =>
            onSelect(
              event.target.checked,
              record[`${props.tableName}Id`],
              record
            )
          }
          checked={
            selectedRows.find((x) => x.id === record[`${props.tableName}Id`]) !=
            null
          }
        ></Checkbox>
      );
    },
  } as DataTableField;
  return (
    <DataTable
      ref={dataTableRef}
      {...props}
      fields={[selectableField, ...(props.fields != null ? props.fields : [])]}
      columnWidths={[
        "32x",
        ...(props.columnWidths != null ? props.columnWidths : []),
      ]}
      tableStyle={{
        truncate: false,
      }}
      onRowClick={(record) => {
        onSelect(
          !selectedRows.some((r) => {
            return r.id === record[`${props.tableName}Id`];
          }),
          record[`${props.tableName}Id`],
          record
        );
        return false;
      }} // optional
      showOptions={false}
      canCreate={false}
      canDelete={false}
      onDataLoaded={(data) => {
        setTableData(data.results);
      }}
    ></DataTable>
  );
};

export default DataTableSelectable;
