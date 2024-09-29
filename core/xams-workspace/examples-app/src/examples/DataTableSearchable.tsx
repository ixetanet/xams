import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableSearchable = () => {
  return (
    <>
      <p>
        Search can be disabled by setting the searchable prop to false. The
        search bar will not be displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" searchable={false} />
      </div>
    </>
  );
};

export default DataTableSearchable;
