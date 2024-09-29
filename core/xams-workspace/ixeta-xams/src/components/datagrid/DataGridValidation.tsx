import { DataGridProps } from "./DataGridTypes";

const DataGridValidation = (props: DataGridProps) => {
  if (props.rows.length > 0) {
    if (
      props.rows[0].columns === undefined ||
      props.rows[0].columns.length === 0
    ) {
      throw new Error("DataGrid: rows must contain columns");
    }
    let numberOfColumns = props.rows[0].columns.length;
    for (let r of props.rows) {
      if (r.columns == null) {
        throw new Error("DataGrid: rows must contain columns");
      }
      if (r.columns.length !== numberOfColumns) {
        throw new Error(
          `DataGrid: rows must contain the same number of columns`
        );
      }
    }
    if (typeof props.columnWidths !== "number") {
      if (props.columnWidths.length !== numberOfColumns) {
        throw new Error(
          `DataGrid: columnWidths ${props.columnWidths} must be the same length as the number of columns ${numberOfColumns}`
        );
      }
    }
  }
};

export default DataGridValidation;
