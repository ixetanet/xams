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

const DataTableMaxResults = () => {
  return (
    <>
      <p>
        The maxResults prop can be used to limit the number of records
        displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" maxResults={5} />
      </div>
    </>
  );
};

export default DataTableMaxResults;

`;

const DataTableMaxResultsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableMaxResultsCode;
