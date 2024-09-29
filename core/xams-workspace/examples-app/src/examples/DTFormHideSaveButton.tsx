import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormHideSaveButton = () => {
  return (
    <>
      <p>
        The formHideSaveButton prop can be used to hide the save button on the
        form that opens when clicking the Add button or on a row.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formHideSaveButton={true} />
      </div>
    </>
  );
};

export default DTFormHideSaveButton;
