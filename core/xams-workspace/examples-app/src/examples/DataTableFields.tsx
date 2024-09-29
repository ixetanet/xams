import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableFields = () => {
  return (
    <>
      <p>
        You can specify the DataTable fields and their ordering by using the
        fields prop.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" fields={["Price", "Name"]} />
      </div>
    </>
  );
};

export default DataTableFields;
