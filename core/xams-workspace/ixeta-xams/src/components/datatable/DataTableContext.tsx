import React from "react";
import { DataTableShape } from "./DataTableTypes";

export const DataTableContext = React.createContext<DataTableShape | null>(
  null
);

export const useDataTableContext = () => {
  const context = React.useContext(DataTableContext);
  if (context == null) {
    throw new Error(
      "useDataTableContext must be used within a DataTableContextProvider"
    );
  }
  return context;
};
