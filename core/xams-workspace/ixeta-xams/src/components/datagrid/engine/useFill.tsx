import React, { useEffect, useRef, useState } from "react";
import { CellLocation, CellRange, DataGridProps } from "../DataGridTypes";
import { useMergedCellsType } from "./useMergedCells";

interface useFillProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
  selectedRange: CellRange | null;
  setSelectedRange: React.Dispatch<React.SetStateAction<CellRange | null>>;
  isEditing: boolean;
  columnWidths: number[];
  rowHeights: number[];
  snapRows: number;
  snapColumns: number;
  scrollRef: React.RefObject<HTMLDivElement | null>;
  mergedCells: useMergedCellsType;
  expandRangeToIncludeMergedCells: (range: CellRange) => CellRange;
}

type FillDragState = {
  // Normalized and merge-expanded at mousedown, frozen for the whole drag
  source: CellRange;
  // Normalized and merge-expanded; null while the pointer is inside the source
  target: CellRange | null;
};

const sum = (values: number[], from: number, to: number) => {
  let total = 0;
  for (let i = from; i < to; i++) {
    total += values[i] ?? 0;
  }
  return total;
};

// Index of the row/column whose pixel span contains pos, clamped to the last
const locate = (sizes: number[], pos: number) => {
  let acc = 0;
  for (let i = 0; i < sizes.length; i++) {
    acc += sizes[i];
    if (pos < acc) return i;
  }
  return sizes.length - 1;
};

const normalize = (range: CellRange): CellRange => ({
  start: {
    row: Math.min(range.start.row, range.end.row),
    col: Math.min(range.start.col, range.end.col),
  },
  end: {
    row: Math.max(range.start.row, range.end.row),
    col: Math.max(range.start.col, range.end.col),
  },
});

const union = (a: CellRange, b: CellRange): CellRange => ({
  start: {
    row: Math.min(a.start.row, b.start.row),
    col: Math.min(a.start.col, b.start.col),
  },
  end: {
    row: Math.max(a.end.row, b.end.row),
    col: Math.max(a.end.col, b.end.col),
  },
});

const sameRange = (a: CellRange | null, b: CellRange | null) => {
  if (a == null || b == null) return a === b;
  return (
    a.start.row === b.start.row &&
    a.start.col === b.start.col &&
    a.end.row === b.end.row &&
    a.end.col === b.end.col
  );
};

const useFill = (params: useFillProps) => {
  const { props, mergedCells } = params;

  const [dragState, setDragState] = useState<FillDragState | null>(null);
  // The release ending a drag synthesizes a click on the cell under the
  // pointer; Cell checks this flag so that click can't clear the selection
  const suppressClickRef = useRef(false);

  const handleVisible =
    props.onFill != null && props.editable !== false && !params.isEditing;

  const startFillDrag = (e: React.MouseEvent) => {
    if (!handleVisible) return;
    const base =
      params.selectedRange ??
      (params.activeCell != null
        ? { start: params.activeCell, end: params.activeCell }
        : null);
    if (base == null) return;
    // preventDefault stops the drag extending native text selection;
    // stopPropagation keeps the cell's own mousedown handling out of it
    e.preventDefault();
    e.stopPropagation();
    suppressClickRef.current = true;
    setDragState({
      source: params.expandRangeToIncludeMergedCells(normalize(base)),
      target: null,
    });
  };

  const onDragMove = (e: MouseEvent) => {
    e.preventDefault();
    const el = params.scrollRef.current;
    const ds = dragState;
    if (el == null || ds == null) return;
    const rect = el.getBoundingClientRect();
    const rawX = e.clientX - rect.left;
    const rawY = e.clientY - rect.top;

    // Nudge the scrollport when the pointer nears an edge; the coordinate
    // hit-testing below resolves scrolled-to cells even though virtualization
    // never rendered them. Holding the pointer still past the edge does not
    // keep scrolling — that would need a timer loop.
    const scrollEdge = 24;
    const scrollStep = 16;
    const nudge = { left: 0, top: 0 };
    if (rawX > el.clientWidth - scrollEdge) nudge.left = scrollStep;
    else if (rawX < scrollEdge) nudge.left = -scrollStep;
    if (rawY > el.clientHeight - scrollEdge) nudge.top = scrollStep;
    else if (rawY < scrollEdge) nudge.top = -scrollStep;
    if (nudge.left !== 0 || nudge.top !== 0) {
      el.scrollBy(nudge);
    }

    // A pointer over a frozen band is over unscrolled cells, which occupy
    // [0, snapWidth/snapHeight) in content coordinates
    const x = Math.min(Math.max(rawX, 0), el.clientWidth);
    const y = Math.min(Math.max(rawY, 0), el.clientHeight);
    const snapWidth = sum(params.columnWidths, 0, params.snapColumns);
    const snapHeight = sum(params.rowHeights, 0, params.snapRows);
    const contentX = x < snapWidth ? x : x + el.scrollLeft;
    const contentY = y < snapHeight ? y : y + el.scrollTop;

    const src = ds.source;
    const srcLeft = sum(params.columnWidths, 0, src.start.col);
    const srcRight = sum(params.columnWidths, 0, src.end.col + 1);
    const srcTop = sum(params.rowHeights, 0, src.start.row);
    const srcBottom = sum(params.rowHeights, 0, src.end.row + 1);
    const dx =
      contentX < srcLeft
        ? srcLeft - contentX
        : contentX > srcRight
        ? contentX - srcRight
        : 0;
    const dy =
      contentY < srcTop
        ? srcTop - contentY
        : contentY > srcBottom
        ? contentY - srcBottom
        : 0;

    let target: CellRange | null = null;
    if (dx !== 0 || dy !== 0) {
      const pointerRow = locate(params.rowHeights, contentY);
      const pointerCol = locate(params.columnWidths, contentX);
      // Pixel distance decides the fill axis — cell counts would favor
      // horizontal on typical wide-column/short-row grids. Ties go vertical
      // because fill-down is the common gesture.
      if (dy >= dx) {
        target =
          pointerRow < src.start.row
            ? {
                start: { row: pointerRow, col: src.start.col },
                end: { row: src.start.row - 1, col: src.end.col },
              }
            : {
                start: { row: src.end.row + 1, col: src.start.col },
                end: { row: pointerRow, col: src.end.col },
              };
      } else {
        target =
          pointerCol < src.start.col
            ? {
                start: { row: src.start.row, col: pointerCol },
                end: { row: src.end.row, col: src.start.col - 1 },
              }
            : {
                start: { row: src.start.row, col: src.end.col + 1 },
                end: { row: src.end.row, col: pointerCol },
              };
      }
      // The pointer can sit beyond the source in pixels while its cell is
      // still a source edge cell (clamped at the grid boundary) — no room
      if (target.start.row > target.end.row || target.start.col > target.end.col) {
        target = null;
      } else {
        target = params.expandRangeToIncludeMergedCells(target);
      }
    }

    setDragState((prev) =>
      prev == null || sameRange(prev.target, target) ? prev : { ...prev, target }
    );
  };

  const onDragEnd = () => {
    const ds = dragState;
    if (ds != null && ds.target != null && props.onFill != null) {
      const src = ds.source;
      const tgt = ds.target;
      const srcRows = src.end.row - src.start.row + 1;
      const srcCols = src.end.col - src.start.col + 1;
      // Euclidean modulo of the absolute offset continues the source pattern
      // periodically in every direction — the row above a source [a,b,c]
      // receives c, matching spreadsheet fill behavior
      const mod = (n: number, m: number) => ((n % m) + m) % m;
      const data: any[][] = [];
      const value: (string | undefined)[][] = [];
      for (let r = tgt.start.row; r <= tgt.end.row; r++) {
        const gridRow = props.rows[r];
        if (gridRow == null) break; // clip rows past the end of the grid
        const dataRow: any[] = [];
        const valueRow: (string | undefined)[] = [];
        for (let c = tgt.start.col; c <= tgt.end.col; c++) {
          const cell = gridRow.columns[c];
          if (cell == null) break; // clip columns past the end of the grid
          if (cell.isReadOnly || cell.isDisabled) {
            // Overlapping a read-only/disabled cell fills nothing in that cell
            dataRow.push(undefined);
            valueRow.push(undefined);
          } else {
            const srcCell =
              props.rows[src.start.row + mod(r - src.start.row, srcRows)]
                ?.columns[src.start.col + mod(c - src.start.col, srcCols)];
            dataRow.push(cell.data);
            valueRow.push(srcCell?.value);
          }
        }
        data.push(dataRow);
        value.push(valueRow);
      }
      props.onFill(data, value, src, tgt);
      // The selection grows over the filled block; the active cell stays put
      params.setSelectedRange(union(src, tgt));
    }
    setDragState(null);
    // The release's synthesized click is dispatched before this timeout runs,
    // so a release on a cell is swallowed while the user's next real click
    // still works even when the release happened off-grid
    setTimeout(() => {
      suppressClickRef.current = false;
    }, 0);
  };

  const onDragAbort = () => {
    setDragState(null);
    // The mouse is still down after Escape — keep swallowing until the
    // eventual release's click has been dispatched
    window.addEventListener(
      "mouseup",
      () => {
        setTimeout(() => {
          suppressClickRef.current = false;
        }, 0);
      },
      { once: true }
    );
  };

  const handlersRef = useRef({ onDragMove, onDragEnd, onDragAbort });
  handlersRef.current = { onDragMove, onDragEnd, onDragAbort };

  const isFillDragging = dragState != null;
  // Keyed on the boolean so per-cell target updates don't churn the listeners
  useEffect(() => {
    if (!isFillDragging) return;
    const move = (e: MouseEvent) => handlersRef.current.onDragMove(e);
    const up = () => handlersRef.current.onDragEnd();
    const key = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        handlersRef.current.onDragAbort();
      }
    };
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
    window.addEventListener("keydown", key);
    const previousCursor = document.body.style.cursor;
    document.body.style.cursor = "crosshair";
    return () => {
      window.removeEventListener("mousemove", move);
      window.removeEventListener("mouseup", up);
      window.removeEventListener("keydown", key);
      document.body.style.cursor = previousCursor;
    };
  }, [isFillDragging]);

  // Which edges of the source∪target block this cell's rendered rect sits on
  // during a drag — drives the dashed preview perimeter. The union is also
  // exactly the selection the fill commits. Same merged-cell edge logic as
  // getRangeEdges: a merged cell renders as one rect, so its far edges are
  // the region's end row/col.
  const getFillPreviewEdges = (
    row: number,
    col: number
  ): { top: boolean; right: boolean; bottom: boolean; left: boolean } | null => {
    if (dragState == null || dragState.target == null) return null;
    const preview = union(dragState.source, dragState.target);
    if (
      row < preview.start.row ||
      row > preview.end.row ||
      col < preview.start.col ||
      col > preview.end.col
    ) {
      return null;
    }
    const merged = mergedCells.getCellMergeInfo(row, col);
    const edges = {
      top: row === preview.start.row,
      left: col === preview.start.col,
      bottom: (merged ? merged.end.row : row) === preview.end.row,
      right: (merged ? merged.end.col : col) === preview.end.col,
    };
    return edges.top || edges.left || edges.bottom || edges.right
      ? edges
      : null;
  };

  const shouldSuppressClick = () => suppressClickRef.current;

  return {
    handleVisible,
    isFillDragging,
    startFillDrag,
    getFillPreviewEdges,
    shouldSuppressClick,
  };
};

export default useFill;
export type useFillType = ReturnType<typeof useFill>;
