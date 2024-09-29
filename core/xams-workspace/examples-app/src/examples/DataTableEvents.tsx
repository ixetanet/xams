import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableEvents = () => {
  return (
    <>
      <p>
        The DataTable component provides several events that can be used to
        interact with the data. These events can be used to execute code when
        the data is loaded, when a row is clicked, when a page is changed, and
        when a record is deleted.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          // Executes once on mount
          onInitialLoad={(records) => {
            console.log("initial load", records);
          }}
          // Executes every time the data is loaded
          onDataLoaded={(records) => {
            console.log("data loaded", records);
          }}
          onPostDelete={(record) => {
            console.log("post delete", record.Name);
          }}
          onRowClick={(record) => {
            console.log("row click", record);
            return true; // Open the form, false to not open the form
          }}
          onPageChange={(pageNumber) => {
            console.log("page change", pageNumber);
          }}
        />
      </div>
    </>
  );
};

export default DataTableEvents;
