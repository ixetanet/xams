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
import { Button, Grid } from "@mantine/core";
import React from "react";

const FormBuilderSetField = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The setField method can be used to set the value of a field in the form.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full">
          <div className="font-bold">Widget Form</div>
          <Grid>
            <Grid.Col span={6}>
              <Field name="Name" />
            </Grid.Col>
            <Grid.Col span={6}>
              <Field name="Price" />
            </Grid.Col>
          </Grid>
          <div className="w-full flex gap-2">
            <Button
              onClick={() => formBuilder.setField("Name", "The Widget 3000")}
            >
              Set Name Field!
            </Button>
            <SaveButton />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FormBuilderSetField;
`;

const FormBuilderSetFieldCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default FormBuilderSetFieldCode;
