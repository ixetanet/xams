import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableScrollable = () => {
  return (
    <>
      <p>
        If the scrollable prop is set to false, the DataTable will not be
        scrollable. The height of the DataTable will be determined by the number
        of records displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" scrollable={false} />
      </div>
    </>
  );
};

export default DataTableScrollable;
