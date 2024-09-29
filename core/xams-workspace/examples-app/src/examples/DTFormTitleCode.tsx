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

const DTFormTitle = () => {
  return (
    <>
      <p>
        The formTitle prop can be used to customize the title of the form that
        opens when clicking the Add button or on a row.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formTitle="Make a Widget!" />
      </div>
    </>
  );
};

export default DTFormTitle;

`;

const DTFormTitleCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormTitleCode;
