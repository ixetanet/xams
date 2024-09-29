import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FormBuilder = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The useFormBuilder hook enables the creation of forms using fields
        defined in your Entity Framework models.
      </p>
      <p>
        To utilize the useFormBuilder hook, specify the table name, pass the
        formBuilder object to the FormContainer, and use the Field component to
        render the fields.
      </p>
      <p>The SaveButton component will save the form data to the database.</p>
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

export default FormBuilder;
