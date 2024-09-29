import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormCloseOptions = () => {
  return (
    <>
      <p>
        The formCloseOnCreate, formCloseOnEscape, and formCloseOnUpdate props
        can be used to control the behavior of the form when creating, updating,
        or when the escape key is pressed. By default, the form will close after
        creating or updating a record, and when the escape key is pressed.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          formCloseOnCreate={false}
          formCloseOnEscape={false}
          formCloseOnUpdate={false}
        />
      </div>
    </>
  );
};

export default DTFormCloseOptions;
