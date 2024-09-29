import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableDisabled = () => {
  return (
    <>
      <p>The DataTable can be disabled by setting the disabledMessage prop.</p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          disabledMessage="This table is disabled."
        />
      </div>
    </>
  );
};

export default DataTableDisabled;
