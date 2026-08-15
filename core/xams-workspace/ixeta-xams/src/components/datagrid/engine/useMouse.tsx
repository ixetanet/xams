import React from "react";
import { CellLocation, CellRange, DataGridProps } from "../DataGridTypes";
import { useMergedCellsType } from "./useMergedCells";

interface useMouseProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
  setActiveCell: React.Dispatch<React.SetStateAction<CellLocation | undefined>>;
  isEditing: boolean;
  onEndEdit: () => void;
  selectedRange: CellRange | null;
  setSelectedRange: React.Dispatch<React.SetStateAction<CellRange | null>>;
  mergedCells: useMergedCellsType;
}

const useMouse = (params: useMouseProps) => {
  const { props, mergedCells } = params;

  // A shift-click range grows to fully contain any merged region it touches
  const expandRangeToIncludeMergedCells = (range: CellRange): CellRange => {
    let minRow = Math.min(range.start.row, range.end.row);
    let maxRow = Math.max(range.start.row, range.end.row);
    let minCol = Math.min(range.start.col, range.end.col);
    let maxCol = Math.max(range.start.col, range.end.col);

    for (let r = minRow; r <= maxRow; r++) {
      for (let c = minCol; c <= maxCol; c++) {
        const mergedCell = mergedCells.getCellMergeInfo(r, c);
        if (mergedCell) {
          minRow = Math.min(minRow, mergedCell.start.row);
          maxRow = Math.max(maxRow, mergedCell.end.row);
          minCol = Math.min(minCol, mergedCell.start.col);
          maxCol = Math.max(maxCol, mergedCell.end.col);
        }
      }
    }

    return {
      start: { row: minRow, col: minCol },
      end: { row: maxRow, col: maxCol },
    };
  };

  const onCellClick = (cellLocation: CellLocation, shiftKey: boolean) => {
    const cell = props.rows[cellLocation.row]?.columns[cellLocation.col];
    if (cell == null) return;

    // Per-cell onClick fires even when the grid is not editable
    if (cell.onClick != null) {
      cell.onClick(cell.value ?? "", cellLocation, cell.data);
    }

    if (props.editable === false) {
      return;
    }

    const merged = mergedCells.getCellMergeInfo(
      cellLocation.row,
      cellLocation.col
    );
    const target = merged ? { ...merged.primaryCell } : cellLocation;

    if (shiftKey && params.activeCell != null) {
      params.setSelectedRange(
        expandRangeToIncludeMergedCells({
          start: params.activeCell,
          end: target,
        })
      );
      return;
    }

    params.setSelectedRange(null);
    params.setActiveCell(target);
    if (params.isEditing) {
      params.onEndEdit();
    }
  };

  const isCellInRange = (row: number, col: number): boolean => {
    // editable={false} shows no selection visuals of any kind
    if (props.editable === false) return false;
    if (params.selectedRange == null) return false;
    const { start, end } = params.selectedRange;
    const minRow = Math.min(start.row, end.row);
    const maxRow = Math.max(start.row, end.row);
    const minCol = Math.min(start.col, end.col);
    const maxCol = Math.max(start.col, end.col);
    return row >= minRow && row <= maxRow && col >= minCol && col <= maxCol;
  };

  // Which edges of the selected range this cell's rendered rect sits on —
  // drives the perimeter border around the selection. A merged cell renders
  // as one rect spanning its whole region, so its far edges are the region's
  // end row/col. Interior cells return null.
  const getRangeEdges = (
    row: number,
    col: number
  ): { top: boolean; right: boolean; bottom: boolean; left: boolean } | null => {
    if (!isCellInRange(row, col)) return null;
    const { start, end } = params.selectedRange!;
    const minRow = Math.min(start.row, end.row);
    const maxRow = Math.max(start.row, end.row);
    const minCol = Math.min(start.col, end.col);
    const maxCol = Math.max(start.col, end.col);
    const merged = mergedCells.getCellMergeInfo(row, col);
    const edges = {
      top: row === minRow,
      left: col === minCol,
      bottom: (merged ? merged.end.row : row) === maxRow,
      right: (merged ? merged.end.col : col) === maxCol,
    };
    return edges.top || edges.left || edges.bottom || edges.right
      ? edges
      : null;
  };

  return {
    onCellClick,
    isCellInRange,
    getRangeEdges,
    expandRangeToIncludeMergedCells,
  };
};

export default useMouse;
export type useMouseType = ReturnType<typeof useMouse>;
