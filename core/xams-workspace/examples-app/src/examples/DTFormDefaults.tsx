import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormDefaults = () => {
  return (
    <>
      <p>
        The formFieldDefaults prop can be used to set default values for the
        form fields on create. This works even if the fields are hidden.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          formFieldDefaults={[
            {
              field: "Price",
              value: 100,
            },
          ]}
        />
      </div>
    </>
  );
};

export default DTFormDefaults;
