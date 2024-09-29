import Highlighter from "@/components/Highlighter";
import React from "react";

const csharp = `[Table("Widget")]
public class Widget
{
    public Guid WidgetId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}`;

const tsx = `import { DataTable, Field, SaveButton } from "@ixeta/xams";
import { Button } from "@mantine/core";
import React from "react";

const DTFormCustom = () => {
  return (
    <>
      <p>
        The customForm prop can be used to customize the form that opens when
        clicking the Add button or on a row.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          customForm={(formBuilder, disclosure) => {
            return (
              <div className="w-full flex flex-col">
                <Field name="Name" />
                <div>
                  The name of this widget will be: {formBuilder.data?.Name}
                </div>
                <div className="w-full flex gap-2">
                  <Button onClick={disclosure.close}>Close</Button>
                  <SaveButton />
                </div>
              </div>
            );
          }}
        />
      </div>
    </>
  );
};

export default DTFormCustom;

`;

const DTFormCustomCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormCustomCode;
