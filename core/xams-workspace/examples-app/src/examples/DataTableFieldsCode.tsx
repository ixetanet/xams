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

const DataTableFields = () => {
  return (
    <>
      <p>
        You can specify the DataTable fields and their ordering by using the
        fields prop.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" fields={["Price", "Name"]} />
      </div>
    </>
  );
};

export default DataTableFields;
`;

const DataTableFieldsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableFieldsCode;
