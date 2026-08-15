import React, { useCallback } from "react";
import { CellRange, MergedCell } from "../DataGridTypes";

export type MergedCellsMap = Map<string, MergedCell>;

const getMergedCellKey = (
  startRow: number,
  startCol: number,
  endRow: number,
  endCol: number
): string => {
  return `${startRow}-${startCol}-${endRow}-${endCol}`;
};

interface useMergedCellsProps {
  mergedCells: MergedCellsMap;
  // Mirrors the latest map synchronously so several imperative
  // mergeCells/unmergeCells calls in one task compose instead of each
  // cloning the state captured by the current render (React batches the
  // setState calls, so only the last clone would survive otherwise).
  mergedCellsRef: React.MutableRefObject<MergedCellsMap>;
  setMergedCells: React.Dispatch<React.SetStateAction<MergedCellsMap>>;
  snapRows: number;
  snapColumns: number;
}

const useMergedCells = (props: useMergedCellsProps) => {
  const { mergedCells, mergedCellsRef, setMergedCells, snapRows, snapColumns } =
    props;

  const commit = useCallback(
    (map: MergedCellsMap) => {
      mergedCellsRef.current = map;
      setMergedCells(map);
    },
    [mergedCellsRef, setMergedCells]
  );

  const getCellMergeInfo = useCallback(
    (row: number, col: number): MergedCell | null => {
      for (const [, mergedCell] of mergedCells) {
        const { start, end } = mergedCell;
        if (
          row >= start.row &&
          row <= end.row &&
          col >= start.col &&
          col <= end.col
        ) {
          return mergedCell;
        }
      }
      return null;
    },
    [mergedCells]
  );

  const isPrimaryCellOfMergedRegion = useCallback(
    (row: number, col: number): boolean => {
      const mergedCell = getCellMergeInfo(row, col);
      if (!mergedCell) return false;
      return (
        mergedCell.primaryCell.row === row && mergedCell.primaryCell.col === col
      );
    },
    [getCellMergeInfo]
  );

  const mergeCells = useCallback(
    (range: CellRange): MergedCell | undefined => {
      if (range == null) return undefined;

      const minRow = Math.min(range.start.row, range.end.row);
      const maxRow = Math.max(range.start.row, range.end.row);
      const minCol = Math.min(range.start.col, range.end.col);
      const maxCol = Math.max(range.start.col, range.end.col);

      // A single cell cannot be merged
      if (minRow === maxRow && minCol === maxCol) {
        return undefined;
      }

      // A merged region cannot cross a frozen-pane boundary: each pane
      // (corner, header, frozen column, body) renders independently, so a
      // region spanning panes cannot be displayed as one cell.
      if (
        (minRow < snapRows && maxRow >= snapRows) ||
        (minCol < snapColumns && maxCol >= snapColumns)
      ) {
        return undefined;
      }

      const mergedCell: MergedCell = {
        start: { row: minRow, col: minCol },
        end: { row: maxRow, col: maxCol },
        spanRows: maxRow - minRow + 1,
        spanColumns: maxCol - minCol + 1,
        primaryCell: { row: minRow, col: minCol },
      };

      const currentMergedCells = mergedCellsRef.current;
      const newMergedCells = new Map(currentMergedCells);
      // Merging over existing merged regions replaces them
      for (const [key, existing] of currentMergedCells) {
        if (
          existing.start.row <= maxRow &&
          existing.end.row >= minRow &&
          existing.start.col <= maxCol &&
          existing.end.col >= minCol
        ) {
          newMergedCells.delete(key);
        }
      }
      newMergedCells.set(
        getMergedCellKey(minRow, minCol, maxRow, maxCol),
        mergedCell
      );
      commit(newMergedCells);

      return mergedCell;
    },
    [mergedCellsRef, commit, snapRows, snapColumns]
  );

  const unmergeCells = useCallback(
    (range: CellRange): MergedCell[] => {
      if (range == null) return [];

      const minRow = Math.min(range.start.row, range.end.row);
      const maxRow = Math.max(range.start.row, range.end.row);
      const minCol = Math.min(range.start.col, range.end.col);
      const maxCol = Math.max(range.start.col, range.end.col);

      const currentMergedCells = mergedCellsRef.current;
      const removed: MergedCell[] = [];
      const newMergedCells = new Map(currentMergedCells);
      for (const [key, mergedCell] of currentMergedCells) {
        if (
          mergedCell.start.row <= maxRow &&
          mergedCell.end.row >= minRow &&
          mergedCell.start.col <= maxCol &&
          mergedCell.end.col >= minCol
        ) {
          newMergedCells.delete(key);
          removed.push(mergedCell);
        }
      }

      if (removed.length === 0) {
        return [];
      }
      commit(newMergedCells);
      return removed;
    },
    [mergedCellsRef, commit]
  );

  // Merged regions overlapping the given inclusive index window — used by the
  // render layers to also paint primaries that fell outside the virtual range
  const getMergedCellsIntersecting = useCallback(
    (
      rowStart: number,
      rowEnd: number,
      colStart: number,
      colEnd: number
    ): MergedCell[] => {
      const result: MergedCell[] = [];
      for (const [, mergedCell] of mergedCells) {
        if (
          mergedCell.end.row >= rowStart &&
          mergedCell.start.row <= rowEnd &&
          mergedCell.end.col >= colStart &&
          mergedCell.start.col <= colEnd
        ) {
          result.push(mergedCell);
        }
      }
      return result;
    },
    [mergedCells]
  );

  const calculateMergedCellWidth = useCallback(
    (mergedCell: MergedCell, columnWidths: number[]): number => {
      let width = 0;
      for (let c = mergedCell.start.col; c <= mergedCell.end.col; c++) {
        width += columnWidths[c] ?? 100;
      }
      return width;
    },
    []
  );

  const calculateMergedCellHeight = useCallback(
    (mergedCell: MergedCell, rowHeights: number[]): number => {
      let height = 0;
      for (let r = mergedCell.start.row; r <= mergedCell.end.row; r++) {
        height += rowHeights[r] ?? 30;
      }
      return height;
    },
    []
  );

  return {
    mergedCells,
    mergeCells,
    unmergeCells,
    getCellMergeInfo,
    isPrimaryCellOfMergedRegion,
    getMergedCellsIntersecting,
    calculateMergedCellWidth,
    calculateMergedCellHeight,
  };
};

export default useMergedCells;
export type useMergedCellsType = ReturnType<typeof useMergedCells>;
