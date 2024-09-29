import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormFields = () => {
  return (
    <>
      <p>
        The fields displayed in the DataTable form can be customized by passing
        an array of field names to the formFields prop.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formFields={["Price"]} />
      </div>
    </>
  );
};

export default DTFormFields;
