import React, { useState } from "react";
import { Cell, CellLocation, DataGridProps } from "./DataGridTypes";
import { useDataGridCellSizeType } from "./useDataGridCellSize";

export interface VGridContextProps {
  children?: any;
}

export type DataGridContextShape = {
  props: DataGridProps;
  editValue: string;
  setEditValue: (value: string) => void;
  activeCell?: CellLocation;
  setActiveCell: React.Dispatch<React.SetStateAction<CellLocation | undefined>>;
  isEditing: boolean;
  setIsEditing: React.Dispatch<React.SetStateAction<boolean>>;
  onKeyDown: (w: Window, e?: KeyboardEvent, value?: string) => void;
  onEndEdit: () => void;
  gridResize: useDataGridCellSizeType;
};

export const DataGridContext = React.createContext<DataGridContextShape | null>(
  null
);

export const useDataGridContext = () => {
  const context = React.useContext(DataGridContext);
  if (!context) {
    throw new Error(
      "useDataGridContext must be used within a DataGridContextProvider"
    );
  }
  return context;
};
