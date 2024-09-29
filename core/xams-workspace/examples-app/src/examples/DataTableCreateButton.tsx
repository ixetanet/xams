import { DataTable } from "@ixeta/xams";
import { Button } from "@mantine/core";
import React from "react";

const DataTableCreateButton = () => {
  return (
    <>
      <p>
        The customCreateButton prop can be used to customize the create button
        that opens the form to create a new record.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          customCreateButton={(open) => {
            return <Button onClick={open}>CREATE!</Button>;
          }}
        />
      </div>
    </>
  );
};

export default DataTableCreateButton;
