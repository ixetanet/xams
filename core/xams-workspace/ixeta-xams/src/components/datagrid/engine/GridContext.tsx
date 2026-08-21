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
  resizing: useResizingType;
  mergedCells: useMergedCellsType;
  fill: useFillType;
  copy: useCopyType;
};

// The context value everything below DataGrid renders from. Its identity is
// memoized and only changes when grid data or interaction state changes —
// scroll-frame re-renders keep the same value, which is what lets memoized
// cells bail out. The virtualizer output changes every scroll frame, so it
// deliberately lives in its own context below, consumed only by the layout
// components (VirtualGrid, sticky panes), never by Cell.
export const GridContext = React.createContext<GridContextShape | null>(null);

export const useGridContext = () => {
  const context = React.useContext(GridContext);
  if (!context) {
    throw new Error("useGridContext must be used within a GridContextProvider");
  }
  return context;
};

export const VirtualizedContext = React.createContext<useVirtualizedType | null>(
  null
);

export const useVirtualizedContext = () => {
  const context = React.useContext(VirtualizedContext);
  if (!context) {
    throw new Error(
      "useVirtualizedContext must be used within a VirtualizedContext provider"
    );
  }
  return context;
};
