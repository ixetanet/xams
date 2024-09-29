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

const FormBuilder = () => {
  const formBuilder = useFormBuilder<any>({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The data property on the formBuilder object contains the form data. The
        data property is updated as the user interacts with the form.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <Grid>
            <Grid.Col span={6}>
              <Field name="Name" />
            </Grid.Col>
          </Grid>
          <div>The widget name will be: {formBuilder.data?.Name}</div>
          <SaveButton />
        </div>
      </FormContainer>
    </>
  );
};

export default FormBuilder;

`;

const FormBuilderDataCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default FormBuilderDataCode;
