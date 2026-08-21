import React, { useCallback, useEffect, useMemo, useRef } from "react";
import { CellLocation, CellRange, DataGridProps } from "../DataGridTypes";
import { useMergedCellsType } from "./useMergedCells";

interface useCopyProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
  selectedRange: CellRange | null;
  isEditing: boolean;
  copiedRange: CellRange | null;
  setCopiedRange: React.Dispatch<React.SetStateAction<CellRange | null>>;
  mergedCells: useMergedCellsType;
  expandRangeToIncludeMergedCells: (range: CellRange) => CellRange;
}

// Inverse of parseClipboardText: cells containing tabs/newlines/quotes are
// wrapped in double quotes with embedded quotes doubled, so Excel and other
// spreadsheets read the matrix back intact
export const serializeClipboardText = (matrix: string[][]): string =>
  matrix
    .map((row) =>
      row
        .map((cell) =>
          /[\t\r\n"]/.test(cell) ? '"' + cell.replaceAll('"', '""') + '"' : cell
        )
        .join("\t")
    )
    .join("\n");

const useCopy = (params: useCopyProps) => {
  const { props, mergedCells } = params;

  const onCopy = (e: ClipboardEvent) => {
    if (props.editable === false) return;
    // While editing, the native edit input owns the copy
    if (params.activeCell == null || params.isEditing) return;
    const base = params.selectedRange ?? {
      start: params.activeCell,
      end: params.activeCell,
    };
    const range = params.expandRangeToIncludeMergedCells({
      start: {
        row: Math.min(base.start.row, base.end.row),
        col: Math.min(base.start.col, base.end.col),
      },
      end: {
        row: Math.max(base.start.row, base.end.row),
        col: Math.max(base.start.col, base.end.col),
      },
    });
    const matrix: string[][] = [];
    for (let r = range.start.row; r <= range.end.row; r++) {
      const row: string[] = [];
      for (let c = range.start.col; c <= range.end.col; c++) {
        row.push(props.rows[r]?.columns[c]?.value ?? "");
      }
      matrix.push(row);
    }
    e.clipboardData?.setData("text/plain", serializeClipboardText(matrix));
    e.preventDefault();
    params.setCopiedRange(range);
  };

  const onCopyRef = useRef(onCopy);
  onCopyRef.current = onCopy;

  useEffect(() => {
    const copyEvent = (e: ClipboardEvent) => {
      onCopyRef.current(e);
    };
    window.addEventListener("copy", copyEvent);
    return () => {
      window.removeEventListener("copy", copyEvent);
    };
  }, []);

  const onEscape = () => {
    // Escape while editing cancels the edit (useKeyboard); the copied
    // border survives until the next Escape, like Google Sheets
    if (params.isEditing) return;
    params.setCopiedRange(null);
  };

  const onEscapeRef = useRef(onEscape);
  onEscapeRef.current = onEscape;

  const hasCopiedRange = params.copiedRange != null;
  useEffect(() => {
    if (!hasCopiedRange) return;
    const keyDownEvent = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onEscapeRef.current();
      }
    };
    window.addEventListener("keydown", keyDownEvent);
    return () => {
      window.removeEventListener("keydown", keyDownEvent);
    };
  }, [hasCopiedRange]);

  // Which edges of the copied block this cell's rendered rect sits on —
  // drives the dashed copy border. Same merged-cell edge logic as
  // getRangeEdges; copiedRange is always normalized.
  const copiedRange = params.copiedRange;
  const getCopiedRangeEdges = useCallback(
    (
      row: number,
      col: number
    ): {
      top: boolean;
      right: boolean;
      bottom: boolean;
      left: boolean;
    } | null => {
      const range = copiedRange;
      if (range == null) return null;
      if (
        row < range.start.row ||
        row > range.end.row ||
        col < range.start.col ||
        col > range.end.col
      ) {
        return null;
      }
      const merged = mergedCells.getCellMergeInfo(row, col);
      const edges = {
        top: row === range.start.row,
        left: col === range.start.col,
        bottom: (merged ? merged.end.row : row) === range.end.row,
        right: (merged ? merged.end.col : col) === range.end.col,
      };
      return edges.top || edges.left || edges.bottom || edges.right
        ? edges
        : null;
    },
    [copiedRange, mergedCells]
  );

  // Stable identity across renders whose inputs didn't change — the grid
  // context value is memoized on this object, so scroll-frame re-renders
  // must not churn it
  return useMemo(
    () => ({
      getCopiedRangeEdges,
    }),
    [getCopiedRangeEdges]
  );
};

export default useCopy;
export type useCopyType = ReturnType<typeof useCopy>;
