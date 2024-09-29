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

const DTFormMinWidth = () => {
  return (
    <>
      <p>
        The formMinWidth prop can be used to set the minimum width of the form.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formMinWidth={500} />
      </div>
    </>
  );
};

export default DTFormMinWidth;
`;

const DTFormMaxWidthCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormMaxWidthCode;
