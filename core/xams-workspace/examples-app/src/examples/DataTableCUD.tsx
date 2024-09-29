import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableCUD = () => {
  return (
    <>
      <p>
        The canCreate, canDelete, and canUpdate props can be used disable the
        ability to create, delete, and update records in the DataTable
        component.
      </p>
      <p>
        By default, all three are set to true. In this example, we set all three
        to false to disable the ability to create, delete, and update records.
      </p>
      <p>
        Security is not enforced on the server side, so it is important to
        implement proper security measures on the server side to prevent
        unauthorized access to the data.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          canCreate={false}
          canDelete={false}
          canUpdate={false}
        />
      </div>
    </>
  );
};

export default DataTableCUD;
