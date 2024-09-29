import React from "react";

export interface SubGridContextProps {
  children?: any;
}

export type SubGridContextShape = {
  skipRows: number;
  skipColumns: number;
};

export const GridSubContext = React.createContext<SubGridContextShape | null>(
  null
);

export const useSubGridContext = () => {
  const context = React.useContext(GridSubContext);
  if (!context) {
    throw new Error("useSubGrid must be used within a SubGridContextProvider");
  }
  return context;
};
