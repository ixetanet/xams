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
import { Button } from "@mantine/core";
import React from "react";

const DataTableCreateButton = () => {
  return (
    <>
      <p>
        The customCreateButton prop can be used to customize the create button
        that opens the form to create a new record.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          customCreateButton={(open) => {
            return <Button onClick={open}>CREATE!</Button>;
          }}
        />
      </div>
    </>
  );
};

export default DataTableCreateButton;
`;

const DataTableCreateButtonCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableCreateButtonCode;
