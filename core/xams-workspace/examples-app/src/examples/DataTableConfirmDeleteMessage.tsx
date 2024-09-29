import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableConfirmDeleteMessage = () => {
  return (
    <>
      <p>
        The deleteConfirmation prop can be used to customize the delete
        confirmation message.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          deleteConfirmation={async (data) => {
            return {
              title: "Delete Widget?",
              message: `Are you sure you want to delete the widget ${data.Name}?`,
            };
          }}
        />
      </div>
    </>
  );
};

export default DataTableConfirmDeleteMessage;
