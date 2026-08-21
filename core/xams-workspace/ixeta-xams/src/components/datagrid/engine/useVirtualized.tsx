import React from "react";
import { useVirtualizer } from "@tanstack/react-virtual";
import { Row } from "../DataGridTypes";

interface useVirtualizedProps {
  rows: Row[];
  columnWidths: number[];
  rowHeights: number[];
  defaultRowHeight: number;
  scrollRef: React.RefObject<HTMLDivElement | null>;
  overscanRows?: number;
  overscanColumns?: number;
}

const useVirtualized = (props: useVirtualizedProps) => {
  const rowVirtualizer = useVirtualizer({
    count: props.rows.length,
    getScrollElement: () => props.scrollRef.current,
    estimateSize: (index) => props.rowHeights[index] ?? props.defaultRowHeight,
    overscan: props.overscanRows ?? 10,
  });

  const columnVirtualizer = useVirtualizer({
    horizontal: true,
    count: props.rows.length > 0 ? props.rows[0].columns.length : 0,
    getScrollElement: () => props.scrollRef.current,
    estimateSize: (index) => props.columnWidths[index] ?? 100,
    overscan: props.overscanColumns ?? 2,
  });

  return {
    rowVirtualizer,
    columnVirtualizer,
    virtualRows: rowVirtualizer.getVirtualItems(),
    virtualColumns: columnVirtualizer.getVirtualItems(),
    totalHeight: rowVirtualizer.getTotalSize(),
    totalWidth: columnVirtualizer.getTotalSize(),
  };
};

export default useVirtualized;
export type useVirtualizedType = ReturnType<typeof useVirtualized>;
