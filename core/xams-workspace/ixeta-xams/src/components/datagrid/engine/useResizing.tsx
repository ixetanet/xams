import { useCallback, useEffect, useMemo, useRef, useState } from "react";

// 32px matches the pre-engine resize clamp; published consumers may rely on it
const MIN_COLUMN_WIDTH = 32;
const MIN_ROW_HEIGHT = 16;

type ResizeState = {
  type: "column" | "row";
  index: number;
  startPosition: number;
  startSize: number;
} | null;

interface useResizingProps {
  columnWidths: number[];
  rowHeights: number[];
  setColumnWidths: (widths: number[]) => void;
  setRowHeights: (heights: number[]) => void;
}

const useResizing = (props: useResizingProps) => {
  const [resizeState, setResizeState] = useState<ResizeState>(null);

  const columnWidthsRef = useRef(props.columnWidths);
  columnWidthsRef.current = props.columnWidths;
  const rowHeightsRef = useRef(props.rowHeights);
  rowHeightsRef.current = props.rowHeights;

  const startColumnResize = useCallback((index: number, clientX: number) => {
    setResizeState({
      type: "column",
      index,
      startPosition: clientX,
      startSize: columnWidthsRef.current[index] ?? MIN_COLUMN_WIDTH,
    });
  }, []);

  const startRowResize = useCallback((index: number, clientY: number) => {
    setResizeState({
      type: "row",
      index,
      startPosition: clientY,
      startSize: rowHeightsRef.current[index] ?? MIN_ROW_HEIGHT,
    });
  }, []);

  useEffect(() => {
    if (resizeState == null) return;

    const onMouseMove = (e: MouseEvent) => {
      e.preventDefault();
      if (resizeState.type === "column") {
        const delta = e.clientX - resizeState.startPosition;
        const size = Math.max(MIN_COLUMN_WIDTH, resizeState.startSize + delta);
        const widths = [...columnWidthsRef.current];
        widths[resizeState.index] = size;
        props.setColumnWidths(widths);
      } else {
        const delta = e.clientY - resizeState.startPosition;
        const size = Math.max(MIN_ROW_HEIGHT, resizeState.startSize + delta);
        const heights = [...rowHeightsRef.current];
        heights[resizeState.index] = size;
        props.setRowHeights(heights);
      }
    };
    const onMouseUp = () => {
      setResizeState(null);
    };

    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);
    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, [resizeState]);

  // Stable identity across renders whose inputs didn't change — the grid
  // context value is memoized on this object, so scroll-frame re-renders
  // must not churn it
  const isResizing = resizeState != null;
  return useMemo(
    () => ({
      isResizing,
      startColumnResize,
      startRowResize,
    }),
    [isResizing, startColumnResize, startRowResize]
  );
};

export default useResizing;
export type useResizingType = ReturnType<typeof useResizing>;
