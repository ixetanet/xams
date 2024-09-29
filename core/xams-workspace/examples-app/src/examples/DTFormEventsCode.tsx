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

const DTFormEvents = () => {
  return (
    <>
      <p>
        The formOnClose, formOnOpen, formOnPreSave, and formOnPostSave events
        can be used to interact with the form that opens when clicking the Add
        button or on a row.
      </p>
      <p>
        The formOnClose event is called when the form is closed. The formOnOpen
        event is called when the form is opened. The formOnPreSave event is
        called before the form is saved. The formOnPostSave event is called
        after the form is saved.
      </p>
      <div className="w-full h-96">
        <DataTable
          tableName="Widget"
          formOnClose={() => {
            console.log("Form closed");
          }}
          formOnOpen={(operation, record) => {
            // Operation is either "CREATE" or "UPDATE"
            console.log("Form opened", operation, record);
          }}
          formOnPreSave={(submissionData) => {
            console.log("Form pre save", submissionData);
          }}
          formOnPostSave={(operation, record) => {
            // Operation is either "CREATE" or "UPDATE"
            console.log("Form post save", operation, record);
          }}
        />
      </div>
    </>
  );
};

export default DTFormEvents;


`;

const DTFormEventsCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default DTFormEventsCode;
