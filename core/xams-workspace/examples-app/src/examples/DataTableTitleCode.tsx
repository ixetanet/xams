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

const DataTableTitle = () => {
  return (
    <>
      <p>The DataTable title can be customized by setting the title prop.</p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" title="My Widgets" />
      </div>
    </>
  );
};

export default DataTableTitle;
`;

const DataTableTitleCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableTitleCode;
