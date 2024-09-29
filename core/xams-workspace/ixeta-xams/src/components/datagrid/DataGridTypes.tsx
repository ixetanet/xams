import { CSSProperties, ReactElement } from "react";

export interface DataGridProps {
  rows: Row[];
  columnWidths: number[] | number;
  onEndEdit?: (value: string, cellLocation: CellLocation, data?: any) => void;
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
  ) => React.ReactNode | React.ReactElement | JSX.Element;
  // overlay?: (
  //   value: string,
  //   cellLocation: CellLocation,
  //   data?: any,
  //   isEditing?: boolean
  // ) => React.ReactNode | React.ReactElement | JSX.Element;
  errorMessage?: string;
  style?: CSSProperties;
  isReadOnly?: boolean;
  isDisabled?: boolean;
  data?: any;
  onChange?: (value: string, cellLocation: CellLocation, data?: any) => void;
  onClick?: (value: string, cellLocation: CellLocation, data?: any) => void;
}

export interface Row {
  columns: Cell[];
}
