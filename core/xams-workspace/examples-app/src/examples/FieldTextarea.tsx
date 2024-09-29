import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FieldTextarea = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The varient prop can be set to textarea to render a textarea input
        field.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <Field name="Name" varient="textarea" />
          <div className="w-full flex flex-col ">
            <SaveButton />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FieldTextarea;
