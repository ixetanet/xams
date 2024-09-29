import React, { useEffect, useState } from "react";
import { Row } from "./DataGridTypes";

interface useDataGridCellSizeProps {
  rows: Row[];
  divRef: React.RefObject<HTMLDivElement>;
  columnWidths: number[];
}

const useDataGridCellSize = (props: useDataGridCellSizeProps) => {
  const [isResizing, setIsResizing] = useState(false);
  const [resizingStartClientX, setResizingStartClientX] = useState<
    number | undefined
  >(undefined);
  const [resizingColIndex, setResizingColIndex] = useState<undefined | number>(
    undefined
  );
  const [initialColumnWidths, setInitialColumnWidths] = useState<number[]>(
    props.columnWidths
  );
  const [columnWidths, setColumnWidths] = useState<number[]>(
    props.columnWidths
  );
  const [mousePosition, setMousePosition] = useState({
    startX: 0,
    startY: 0,
    endY: 0,
    clientX: 0,
    clientY: 0,
  });

  const mouseUpEvent = () => {
    if (isResizing) {
      setIsResizing(false);
      if (resizingColIndex == null) {
        return;
      }
      let amount = mousePosition.clientX - resizingStartClientX!;
      let widths = [...columnWidths];
      let size = widths[resizingColIndex] + amount;
      if (size < 32) {
        size = 32;
      }
      widths[resizingColIndex] = size;
      setColumnWidths(widths);
    }
  };

  const onMouseMove = (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
    if (isResizing) {
      e.preventDefault();
    }

    if (!props.divRef.current) return;

    const rect = props.divRef.current.getBoundingClientRect();

    // Calculate mouse position within the div
    const x = e.clientX - rect.left;
    const y = 0;
    setMousePosition({
      startX: x,
      startY: y,
      endY: rect.height,
      clientX: e.clientX,
      clientY: e.clientY,
    });
  };

  useEffect(() => {
    window.addEventListener("mouseup", mouseUpEvent);
    return () => {
      window.removeEventListener("mouseup", mouseUpEvent);
    };
  });

  useEffect(() => {
    if (props.columnWidths.length !== initialColumnWidths.length) {
      setInitialColumnWidths(props.columnWidths);
      setColumnWidths(props.columnWidths);
    } else {
      // If any of the values in the array have changed, then set
      // the initial widths to the new column widths
      for (let i = 0; i < props.columnWidths.length; i++) {
        if (props.columnWidths[i] !== initialColumnWidths[i]) {
          setInitialColumnWidths(props.columnWidths);
          setColumnWidths(props.columnWidths);
          break;
        }
      }
    }
  }, [props.columnWidths]);

  return {
    isResizing,
    setIsResizing,
    resizingStartClientX,
    setResizingStartClientX,
    resizingColNumber: resizingColIndex,
    setResizingColNumber: setResizingColIndex,
    columnWidths,
    setColWidths: setColumnWidths,
    onMouseMove,
    mousePosition,
  };
};

export default useDataGridCellSize;
export type useDataGridCellSizeType = ReturnType<typeof useDataGridCellSize>;
