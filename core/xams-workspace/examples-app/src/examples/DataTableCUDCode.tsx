import Highlighter from "@/components/Highlighter";
import React from "react";

const csharp = `[Table("Widget")]
public class Widget
{
    public Guid WidgetId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}`;

const tsx = `import { DataTable } from "@ixeta/xams";
import React from "react";

const DataTableCUD = () => {
  return (
    <>
      <p>
        The canCreate, canDelete, and canUpdate props can be used to control
        permissions for the DataTable component. By default, all three are set
        to true. In this example, we set all three to false to disable the
        ability to create, delete, and update records.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          canCreate={false}
          canDelete={false}
          canUpdate={false}
        />
      </div>
    </>
  );
};

export default DataTableCUD;`;

const DataTableCUDCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableCUDCode;
