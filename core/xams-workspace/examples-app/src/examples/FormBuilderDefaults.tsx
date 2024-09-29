import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FormBuilderDefaults = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
    defaults: [
      {
        field: "Name",
        value: "Widget Name",
      },
      {
        field: "Price",
        value: 9.99,
      },
    ],
  });
  return (
    <>
      <p>
        Using the defaults property, you can set default values for fields in
        the form.
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
            <SaveButton />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FormBuilderDefaults;
