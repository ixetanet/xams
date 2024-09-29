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

const FormBuilderValidation = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        Custom form validation can be implemented using the onPreSave callback
        of the SaveButton component. The onPreSave callback is an asynchronous
        function that receives the form data as an argument. If the function
        returns an object with a continue property set to false, the save
        operation will be aborted.
      </p>
      <p>
        Custom validation messages can be set by calling the setFieldError
        method of the formBuilder object.
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
          <div className="w-full flex flex-col pt-4">
            <SaveButton
              onPreSave={async (data) => {
                // Validate the name field is populated
                if (data.Name === "") {
                  formBuilder.setFieldError("Name", "Name is required");
                  return {
                    continue: false,
                  };
                }
                return {
                  continue: true,
                };
              }}
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

export default FormBuilderValidation;
`;

const FormBuilderValidationCode = () => {
  return (
    <div className="w-full flex flex-col">
      c#
      <Highlighter codeBlock={csharp} language="csharp" />
      React
      <Highlighter codeBlock={tsx} language="tsx" />
    </div>
  );
};

export default FormBuilderValidationCode;
