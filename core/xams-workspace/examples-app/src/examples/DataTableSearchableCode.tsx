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

const DataTableSearchable = () => {
  return (
    <>
      <p>
        Search can be disabled by setting the searchable prop to false. The
        search bar will not be displayed.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" searchable={false} />
      </div>
    </>
  );
};

export default DataTableSearchable;
`;

const DataTableSearchableCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableSearchableCode;
