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

const DataTableConfirmDelete = () => {
  return (
    <>
      <p>
        The delete confirmation message can be disabled by setting the
        confirmDelete prop to false.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" confirmDelete={false} />
      </div>
    </>
  );
};

export default DataTableConfirmDelete;

`;

const DataTableConfirmDeleteCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableConfirmDeleteCode;
