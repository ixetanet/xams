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

const DataTables = () => {
  return (
    <>
      <p>
        The DataTable component allows for the display of data in a table
        format. New records can be created by clicking the + button. The
        DataTable supports searching, paging, deletion, and creation.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" />
      </div>
    </>
  );
};

export default DataTables;

`;

const DataTablesCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTablesCode;
