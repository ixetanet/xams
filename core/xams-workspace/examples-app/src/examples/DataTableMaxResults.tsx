import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableMaxResults = () => {
  return (
    <>
      <p>
        The maxResults prop can be used to limit the number of records
        displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" maxResults={5} />
      </div>
    </>
  );
};

export default DataTableMaxResults;
