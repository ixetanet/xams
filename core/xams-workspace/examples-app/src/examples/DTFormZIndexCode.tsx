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

const DTFormZIndex = () => {
  return (
    <>
      <p>
        You might need to adjust the formZIndex prop if the form is not
        appearing above other elements on the page.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formZIndex={100} />
      </div>
    </>
  );
};

export default DTFormZIndex;

`;

const DTFormZIndexCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormZIndexCode;
