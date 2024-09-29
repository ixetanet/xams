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

const DataTableConfirmDeleteMessage = () => {
  return (
    <>
      <p>
        The deleteConfirmation prop can be used to customize the delete
        confirmation message.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          deleteConfirmation={async (data) => {
            return {
              title: "Delete Widget?",
              message: \`Are you sure you want to delete the widget \${data.Name}?\`,
            };
          }}
        />
      </div>
    </>
  );
};

export default DataTableConfirmDeleteMessage;

`;

const DataTableConfirmDeleteMessageCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DataTableConfirmDeleteMessageCode;
