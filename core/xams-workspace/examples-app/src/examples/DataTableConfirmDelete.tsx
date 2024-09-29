import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableConfirmDelete = () => {
  return (
    <>
      <p>
        The delete confirmation message can be disabled by setting the
        confirmDelete prop to false.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" confirmDelete={false} />
      </div>
    </>
  );
};

export default DataTableConfirmDelete;
