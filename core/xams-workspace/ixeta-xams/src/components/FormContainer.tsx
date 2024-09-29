import React, { useEffect } from "react";
import { useFormBuilderType } from "../hooks/useFormBuilder";
import { Loader } from "@mantine/core";
import FormContextProvider from "../contexts/FormContext";

interface FormContainerProps {
  formBuilder: useFormBuilderType;
  className?: string;
  children?: any;
  showLoading?: boolean;
}

const FormContainer = (props: FormContainerProps) => {
  const getIsLoading = () => {
    if (props.formBuilder.isLoading === true) {
      return true;
    }
    if (
      props.formBuilder !== undefined &&
      props.formBuilder.metadata !== undefined &&
      props.formBuilder.data !== undefined
    ) {
      for (const metadataField of props.formBuilder.metadata.fields) {
        if (props.formBuilder.data[metadataField.name] === undefined) {
          console.warn(
            "No data for field: " +
              metadataField.name +
              ". Ensure lookup fields are spelled correctly."
          );
          return true;
        }
      }
    }
    return false;
  };

  const isLoading = getIsLoading() || props.showLoading === true;

  return (
    <FormContextProvider formBuilder={props.formBuilder}>
      <div
        className={`${
          isLoading ||
          props.showLoading === true ||
          !props.formBuilder.canRead.canRead ||
          !props.formBuilder.canCreate
            ? `relative overflow-hidden`
            : ``
        } ${props.className}`}
      >
        {isLoading && (
          <div
            className="absolute w-full h-full flex justify-center items-center"
            style={{
              zIndex: 4000,
            }}
          >
            <Loader></Loader>
          </div>
        )}
        {!isLoading && props.formBuilder.canRead.canRead === false && (
          <div
            className={`absolute w-full h-full flex justify-center items-center text-center`}
            style={{
              zIndex: 10,
            }}
          >
            {props.formBuilder.canRead.message}
          </div>
        )}
        {!isLoading &&
          props.formBuilder.operation === "CREATE" &&
          props.formBuilder.canCreate === false && (
            <div
              className={`absolute w-full h-full flex justify-center items-center text-center`}
              style={{
                zIndex: 10,
              }}
            >
              {`This record doesn't exist or you are missing the required permissions to create ${props.formBuilder.tableName}`}
            </div>
          )}

        <div
          className={`${
            isLoading ||
            props.formBuilder.canRead.canRead === false ||
            (props.formBuilder.operation === "CREATE" &&
              props.formBuilder.canCreate === false)
              ? `invisible`
              : ``
          } ${props.className}`}
        >
          {props.children}
        </div>
      </div>
    </FormContextProvider>
  );
};

export default FormContainer;
