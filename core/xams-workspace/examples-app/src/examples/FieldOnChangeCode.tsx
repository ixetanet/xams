import Highlighter from "@/components/Highlighter";
import React from "react";

const csharp = `[Table("Widget")]
public class Widget
{
    public Guid WidgetId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}`;

const tsx = `import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FieldOnChange = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The onChange prop can be set to a function that will be called when the
        value of the field changes.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <Field
            name="Name"
            onChange={(value) => {
              console.log(value);
            }}
          />
          <div className="w-full flex flex-col ">
            <SaveButton />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FieldOnChange;

`;

const FieldOnChangeCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default FieldOnChangeCode;
