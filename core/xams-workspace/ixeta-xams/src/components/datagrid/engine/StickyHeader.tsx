import React from "react";
import Cell from "../Cell";
import { useGridContext, useVirtualizedContext } from "./GridContext";

const sumTo = (values: number[], to: number) => {
  let total = 0;
  for (let i = 0; i < to; i++) {
    total += values[i] ?? 0;
  }
  return total;
};

// Frozen top rows. A zero-size sticky wrapper keeps the cells pinned
// vertically while they scroll horizontally with the body.
const StickyHeader = () => {
  const gridContext = useGridContext();
  const { rows, snapRows, snapColumns, columnWidths, rowHeights, mergedCells } =
    gridContext;
  const { virtualColumns } = useVirtualizedContext();

  if (snapRows === 0) {
    return null;
  }

  const snapRowOffsets: number[] = [];
  let offset = 0;
  for (let r = 0; r < snapRows; r++) {
    snapRowOffsets.push(offset);
    offset += rowHeights[r] ?? 0;
  }

  const virtualColStart =
    virtualColumns.length > 0 ? virtualColumns[0].index : 0;
  const virtualColEnd =
    virtualColumns.length > 0
      ? virtualColumns[virtualColumns.length - 1].index
      : -1;

  // Wide header merges can outgrow the column overscan: render primaries whose
  // column scrolled out of the virtual window so the visible part still paints
  const outOfRangePrimaries = mergedCells
    .getMergedCellsIntersecting(
      0,
      snapRows - 1,
      Math.max(snapColumns, virtualColStart),
      virtualColEnd
    )
    .filter(
      (mergedCell) =>
        mergedCell.primaryCell.row < snapRows &&
        mergedCell.primaryCell.col >= snapColumns &&
        !(
          mergedCell.primaryCell.col >= virtualColStart &&
          mergedCell.primaryCell.col <= virtualColEnd
        ) &&
        rows[mergedCell.primaryCell.row] != null
    );

  return (
    <div className="sticky top-0 z-30" style={{ width: 0, height: 0 }}>
      {/* flat keyed list — see VirtualGrid for the nested-array remount trap */}
      {Array.from({ length: snapRows }, (_, rowIndex) =>
        virtualColumns.map((column) => {
          if (column.index < snapColumns) {
            return null;
          }
          if (rows[rowIndex] == null) {
            return null;
          }

          const mergedCell = mergedCells.getCellMergeInfo(
            rowIndex,
            column.index
          );
          if (
            mergedCell &&
            !mergedCells.isPrimaryCellOfMergedRegion(rowIndex, column.index)
          ) {
            return null;
          }

          let style: React.CSSProperties = {
            top: snapRowOffsets[rowIndex],
            left: column.start,
            width: columnWidths[column.index] ?? column.size,
            height: rowHeights[rowIndex] ?? 0,
          };
          if (mergedCell) {
            style = {
              ...style,
              width: mergedCells.calculateMergedCellWidth(
                mergedCell,
                columnWidths
              ),
              height: mergedCells.calculateMergedCellHeight(
                mergedCell,
                rowHeights
              ),
            };
          }

          return (
            <Cell
              key={`${rowIndex}-${column.index}`}
              row={rowIndex}
              col={column.index}
              style={style}
            />
          );
        })
      ).flat()}
      {outOfRangePrimaries.map((mergedCell) => (
        <Cell
          key={`${mergedCell.primaryCell.row}-${mergedCell.primaryCell.col}`}
          row={mergedCell.primaryCell.row}
          col={mergedCell.primaryCell.col}
          style={{
            top: snapRowOffsets[mergedCell.primaryCell.row],
            left: sumTo(columnWidths, mergedCell.primaryCell.col),
            width: mergedCells.calculateMergedCellWidth(
              mergedCell,
              columnWidths
            ),
            height: mergedCells.calculateMergedCellHeight(
              mergedCell,
              rowHeights
            ),
          }}
        />
      ))}
    </div>
  );
};

export default StickyHeader;
