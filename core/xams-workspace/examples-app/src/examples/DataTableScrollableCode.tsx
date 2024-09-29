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

const DataTableScrollable = () => {
  return (
    <>
      <p>
        If the scrollable prop is set to false, the DataTable will not be
        scrollable. The height of the DataTable will be determined by the number
        of records displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" scrollable={false} />
      </div>
    </>
  );
};

export default DataTableScrollable;
`;

const DataTableScrollableCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableScrollableCode;
