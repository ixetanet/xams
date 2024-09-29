import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
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
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <Grid>
            <Grid.Col span={6}>
              <Field name="Name" />
            </Grid.Col>
            <Grid.Col span={6}>
              <Field name="Price" />
            </Grid.Col>
          </Grid>
          <Button
            onClick={() => formBuilder.setField("Name", "The Widget 3000")}
          >
            Set Name Field!
          </Button>
          <SaveButton />
        </div>
      </FormContainer>
    </>
  );
};

export default FormBuilderSetField;
