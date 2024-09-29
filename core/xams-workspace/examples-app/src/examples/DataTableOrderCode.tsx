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

const DataTableOrder = () => {
  return (
    <>
      <p>
        The data can be sorted by clicking on the column headers. The default
        order can be set using the orderBy prop.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          orderBy={[
            {
              field: "Price",
              order: "asc",
            },
          ]}
        />
      </div>
    </>
  );
};

export default DataTableOrder;
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
