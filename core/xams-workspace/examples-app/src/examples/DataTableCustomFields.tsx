import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableCustomFields = () => {
  return (
    <>
      <p>
        Custom fields can be added to the DataTable component by passing an
        array of field objects to the fields prop. Each field object must
        contain a header and body property. The header property can be a string
        or a function that returns a React node. The body property must be a
        function that returns a React node.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          fields={[
            "Name",
            {
              header: "My Field 1",
              body: (record) => {
                return <div className=" bg-blue-300">{record.Name}</div>;
              },
            },
            {
              header: () => <div className="bg-red-300">My Field 2</div>,
              body: (record) => {
                return <div className=" bg-green-300">{record.Name}</div>;
              },
            },
          ]}
        />
      </div>
    </>
  );
};

export default DataTableCustomFields;