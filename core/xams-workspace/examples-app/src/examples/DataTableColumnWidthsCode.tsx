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

const DataTableColumnWidths = () => {
  return (
    <>
      <p>
        Column widths can be set using the columnWidths prop. The first column
        width is set to 100px and the second column width is set to 100%.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" columnWidths={["100px", "100%"]} />
      </div>
    </>
  );
};

export default DataTableColumnWidths;

`;

const DataTableColumnWidthsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableColumnWidthsCode;
