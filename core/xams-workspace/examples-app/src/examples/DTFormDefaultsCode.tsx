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

const DTFormDefaults = () => {
  return (
    <>
      <p>
        The formFieldDefaults prop can be used to set default values for the
        form fields on create. This works even if the fields are hidden.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          formFieldDefaults={[
            {
              field: "Price",
              value: 100,
            },
          ]}
        />
      </div>
    </>
  );
};

export default DTFormDefaults;

`;

const DTFormDefaultsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormDefaultsCode;
