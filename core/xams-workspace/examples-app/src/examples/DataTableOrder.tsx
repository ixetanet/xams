import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableOrder = () => {
  return (
    <>
      <p>
        The data can be sorted by clicking on the column headers. The default
        order can be set using the `orderBy` prop.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          orderBy={[
            {
              field: "Price",
              order: "asc",
            },
          ]}
        />
      </div>
    </>
  );
};

export default DataTableOrder;
