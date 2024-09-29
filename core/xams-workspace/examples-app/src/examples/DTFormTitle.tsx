import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormTitle = () => {
  return (
    <>
      <p>
        The formTitle prop can be used to customize the title of the form that
        opens when clicking the Add button or on a row.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formTitle="Make a Widget!" />
      </div>
    </>
  );
};

export default DTFormTitle;
