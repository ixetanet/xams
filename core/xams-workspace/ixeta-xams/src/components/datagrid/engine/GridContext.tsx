import React from "react";
import {
  Cell,
  CellLocation,
  CellRange,
  DataGridProps,
  Row,
} from "../DataGridTypes";
import { useVirtualizedType } from "./useVirtualized";
import { useResizingType } from "./useResizing";
import { useMergedCellsType } from "./useMergedCells";
import { useFillType } from "./useFill";
import { useCopyType } from "./useCopy";

export type GridContextShape = {
  props: DataGridProps;
  rows: Row[];
  columnWidths: number[];
  rowHeights: number[];
  snapRows: number;
  snapColumns: number;
  width: number;
  height: number;
  activeCell?: Cell;
  activeCellLocation?: CellLocation;
  editValue: string;
  setEditValue: (value: string) => void;
  isEditing: boolean;
  selectedRange: CellRange | null;
  onKeyDown: (w: Window, e?: KeyboardEvent, value?: string) => void;
  onEndEdit: () => void;
  onCellClick: (cellLocation: CellLocation, shiftKey: boolean) => void;
  isCellInRange: (row: number, col: number) => boolean;
  getRangeEdges: (
    row: number,
    col: number
  ) => { top: boolean; right: boolean; bottom: boolean; left: boolean } | null;
  virtualized: useVirtualizedType;
  resizing: useResizingType;
  mergedCells: useMergedCellsType;
  fill: useFillType;
  copy: useCopyType;
};

export const GridContext = React.createContext<GridContextShape | null>(null);

export const useGridContext = () => {
  const context = React.useContext(GridContext);
  if (!context) {
    throw new Error("useGridContext must be used within a GridContextProvider");
  }
  return context;
};
