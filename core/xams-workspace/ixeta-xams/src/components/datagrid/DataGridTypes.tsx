import { CSSProperties, ReactElement } from "react";

export interface DataGridProps {
  rows: Row[];
  columnWidths: number[] | number;
  onEndEdit?: (value: string, cellLocation: CellLocation, data?: any) => void;
  onDelete?: (
    value: string | undefined,
    cellLocation: CellLocation,
    data?: any
  ) => void;
  onBackspace?: (
    value: string | undefined,
    cellLocation: CellLocation,
    data?: any
  ) => void;
  snapColumns?: number;
  snapRows?: number;
  zIndex?: number;
  rightAlignNumbers?: boolean;
  editable?: boolean;
  style?: React.CSSProperties;
  styletr?: React.CSSProperties;
  styletl?: React.CSSProperties;
  stylebl?: React.CSSProperties;
}

export interface CellLocation {
  row: number;
  col: number;
}

export interface Cell {
  value?: string;
  custom?: (
    value: string,
    cellLocation: CellLocation,
    data?: any
  ) => React.ReactNode | React.ReactElement;
  errorMessage?: string;
  style?: CSSProperties;
  isReadOnly?: boolean;
  isDisabled?: boolean;
  data?: any;
  onEditing?: (value: string, cellLocation: CellLocation, data?: any) => string;
  onEndEdit?: (value: string, cellLocation: CellLocation, data?: any) => void;
  onClick?: (value: string, cellLocation: CellLocation, data?: any) => void;
  onDelete?: (
    value: string | undefined,
    cellLocation: CellLocation,
    data?: any
  ) => void;
  onBackspace?: (
    value: string | undefined,
    cellLocation: CellLocation,
    data?: any
  ) => void;
}

export interface Row {
  columns: Cell[];
}
