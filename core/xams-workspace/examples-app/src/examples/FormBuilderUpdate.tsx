import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FormBuilderUpdate = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        After saving the data, the form can be converted into an update form by
        calling formBuilder.setSnapshot(data) in the onPostSave callback of the
        SaveButton component.
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

export default FormBuilderUpdate;
