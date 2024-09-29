import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableColumnWidths = () => {
  return (
    <>
      <p>
        Column widths can be set using the columnWidths prop. The first column
        width is set to 100px and the second column width is set to 100%.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" columnWidths={["100px", "100%"]} />
      </div>
    </>
  );
};

export default DataTableColumnWidths;
