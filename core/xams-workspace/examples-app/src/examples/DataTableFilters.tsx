import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableFilters = () => {
  return (
    <>
      <p>
        The filters prop on the DataTable component allows for the filtering of
        records based on the provided criteria. In this example, the DataTable
        is filtered to show only records where the Price is greater than 10.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          filters={[
            {
              field: "Price",
              operator: ">",
              value: "10",
            },
          ]}
        />
      </div>
    </>
  );
};

export default DataTableFilters;
