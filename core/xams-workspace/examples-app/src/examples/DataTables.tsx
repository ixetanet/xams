import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTables = () => {
  return (
    <>
      <p>
        The DataTable component allows for the display of data in a table
        format. New records can be created by clicking the + button. The
        DataTable supports searching, paging, deletion, and creation.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" />
      </div>
    </>
  );
};

export default DataTables;
