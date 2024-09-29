import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormMaxWidth = () => {
  return (
    <>
      <p>
        The formMaxWidth prop can be used to set the maximum width of the form
        in rem.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formMaxWidth={50} />
      </div>
    </>
  );
};

export default DTFormMaxWidth;
