import React from "react";
import { useFormBuilderType } from "../hooks/useFormBuilder";

export type FormContextShape = {
  formBuilder: useFormBuilderType;
};

interface FormContextProps {
  children?: any;
  formBuilder: useFormBuilderType;
}

export const FormContext = React.createContext<FormContextShape | null>(null);

export const useFormContext = () => {
  const context = React.useContext(FormContext);
  if (!context) {
    throw new Error("useFormContext must be used within a FormContextProvider");
  }
  return context;
};

const FormContextProvider = (props: FormContextProps) => {
  return (
    <FormContext.Provider
      value={{
        formBuilder: props.formBuilder,
      }}
    >
      {props.children}
    </FormContext.Provider>
  );
};

export default FormContextProvider;
