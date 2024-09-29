import { DataTable } from "@ixeta/xams";
import React from "react";

const DTFormZIndex = () => {
  return (
    <>
      <p>
        You might need to adjust the formZIndex prop if the form is not
        appearing above other elements on the page.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formZIndex={100} />
      </div>
    </>
  );
};

export default DTFormZIndex;
