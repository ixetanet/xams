import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableTitle = () => {
  return (
    <>
      <p>The DataTable title can be customized by setting the title prop.</p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" title="My Widgets" />
      </div>
    </>
  );
};

export default DataTableTitle;
