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

const FormBuilderSnapshot = () => {
  const formBuilder = useFormBuilder<any>({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The snapshot property on the formBuilder is populated if the form is an
        update operation.
      </p>
      <p>
        It contains the data that was loaded into the form prior to any edits
        being made.
      </p>
      <p>
        Create a record and observe how the snapshot property is populated with
        last saved data.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <Grid>
            <Grid.Col span={12}>
              <Field name="Name" />
            </Grid.Col>
          </Grid>
          <div>Snapshot Name field: {formBuilder.snapshot?.Name}</div>
          <div className="w-full flex flex-col">
            <SaveButton
              onPostSave={(operation, id, data) => {
                formBuilder.setSnapshot(data);
              }}
            />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FormBuilderSnapshot;

`;

const FormBuilderSnapshotCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default FormBuilderSnapshotCode;
