import { Button, Variants } from "@mantine/core";
import React, {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useState,
} from "react";
import useAuthRequest from "../hooks/useAuthRequest";
import { API_DATA_CREATE, API_DATA_UPDATE } from "../apiurls";
import useGuid from "../hooks/useGuid";
import { ValidationMessage } from "../reducers/formbuilderReducer";
import { useFormContext } from "../contexts/FormContext";
import { Ref } from "react";
import { PostSaveEvent, PreSaveEvent } from "../hooks/useFormBuilder";

interface SaveButtonProps {
  label?: string;
  varient?: Variants<
    "filled" | "outline" | "light" | "white" | "default" | "subtle" | "gradient"
  >;
  className?: string;
  onPreValidate?: PreSaveEvent;
  onPreSave?: PreSaveEvent; // If returns false, save will be cancelled
  onPostSave?: PostSaveEvent;
}

const SaveButton = (props: SaveButtonProps) => {
  const formBuilder = useFormContext().formBuilder;
  if (
    (formBuilder.snapshot !== undefined && formBuilder.canUpdate === false) ||
    (formBuilder.snapshot === undefined && formBuilder.canCreate === false)
  ) {
    return <></>;
  }

  return (
    <Button
      className={props.className}
      onClick={() =>
        formBuilder.save(props.onPreValidate, props.onPreSave, props.onPostSave)
      }
      variant={props.varient}
    >
      {props.label !== undefined ? props.label : ""}
      {props.label === undefined &&
        (formBuilder.snapshot !== undefined ? "Update" : "Create")}
    </Button>
  );
};
export default SaveButton;
