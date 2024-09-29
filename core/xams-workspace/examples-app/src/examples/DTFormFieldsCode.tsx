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

const DTFormFields = () => {
  return (
    <>
      <p>
        The fields displayed in the DataTable form can be customized by passing
        an array of field names to the formFields prop.
      </p>
      <div className="w-full h-96">
        <DataTable tableName="Widget" formFields={["Price"]} />
      </div>
    </>
  );
};

export default DTFormFields;

`;

const DTFormFieldsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormFieldsCode;
