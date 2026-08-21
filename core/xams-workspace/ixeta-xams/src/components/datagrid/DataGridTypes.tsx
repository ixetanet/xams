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
  /**
   * Fires when clipboard content (e.g. cells copied from Excel) is pasted
   * while a cell is active and not being edited. `value` holds the pasted
   * cell values and `data` the target cells' `data` property, both indexed
   * as [row][col] relative to `cellLocation` — the top-left of the selected
   * range (or the active cell when nothing is range-selected), where the
   * paste is anchored. Like other spreadsheets, a selection that is an
   * exact multiple of the copied block in both dimensions is filled by
   * tiling the block (a single copied cell fills the whole selection); any
   * other selection receives the block once at the anchor. The matrices are
   * clipped to the grid bounds; positions whose target cell is read-only or
   * disabled are `undefined` in both matrices and must not be pasted. No
   * event fires when the anchor cell itself is read-only or disabled.
   */
  onPaste?: (
    data: any[][],
    value: (string | undefined)[][],
    cellLocation: CellLocation
  ) => void;
  /**
   * Fires when the fill handle (the square at the bottom-right corner of the
   * selection) is dragged and released. The grid tiles the source selection's
   * values across the drag target and reports the result — it computes no
   * series; consumers may substitute their own series logic using
   * `sourceRange`/`targetRange`. `value` holds the tiled source values and
   * `data` the target cells' `data` property, both indexed as [row][col]
   * relative to `targetRange.start` (the source cells are not included).
   * Unlike `selectedRange`, both ranges are normalized (start is the
   * top-left). Positions whose target cell is read-only or disabled are
   * `undefined` in both matrices and must not be filled. The handle only
   * renders when this prop is set and `editable` is not false.
   */
  onFill?: (
    data: any[][],
    value: (string | undefined)[][],
    sourceRange: CellRange,
    targetRange: CellRange
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
  onSelectionChange?: (
    cellLocation: CellLocation | undefined,
    range: CellRange | null
  ) => void;
  onMergeCells?: (mergedCell: MergedCell) => void;
  onUnmergeCells?: (mergedCell: MergedCell) => void;
  resizableRows?: boolean;
  defaultRowHeight?: number;
  /**
   * Rows rendered beyond the visible viewport on each side so fast scrolling
   * doesn't outrun rendering and show blank space. Default 10.
   */
  overscanRows?: number;
  /**
   * Columns rendered beyond the visible viewport on each side. Default 2.
   */
  overscanColumns?: number;
}

export interface CellRange {
  start: CellLocation;
  end: CellLocation;
}

export interface MergedCell {
  start: CellLocation;
  end: CellLocation;
  spanRows: number;
  spanColumns: number;
  primaryCell: CellLocation;
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
  rowHeight?: number;
}
