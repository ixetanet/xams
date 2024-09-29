import { useFormBuilder, FormContainer, Field, SaveButton } from "@ixeta/xams";
import { Grid } from "@mantine/core";
import React from "react";

const FieldRichText = () => {
  const formBuilder = useFormBuilder({
    tableName: "Widget",
  });
  return (
    <>
      <p>
        The varient prop can be set to rich to render a rich text input field.
      </p>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full flex flex-col gap-2">
          <div className="font-bold">Widget Form</div>
          <div className="w-full h-96">
            <Field name="Name" varient="rich" />
          </div>
          <div className="w-full flex flex-col ">
            <SaveButton />
          </div>
        </div>
      </FormContainer>
    </>
  );
};

export default FieldRichText;
