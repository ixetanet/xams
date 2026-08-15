import React from "react";
import Cell from "../Cell";
import { useGridContext } from "./GridContext";
import { MergedCell } from "../DataGridTypes";

const sumTo = (values: number[], to: number) => {
  let total = 0;
  for (let i = 0; i < to; i++) {
    total += values[i] ?? 0;
  }
  return total;
};

const VirtualGrid = () => {
  const gridContext = useGridContext();
  const { snapRows, snapColumns, columnWidths, rowHeights, mergedCells } =
    gridContext;
  const { virtualRows, virtualColumns } = gridContext.virtualized;

  // Virtual items are contiguous, so containment is an index-range check
  const virtualRowStart = virtualRows.length > 0 ? virtualRows[0].index : 0;
  const virtualRowEnd =
    virtualRows.length > 0 ? virtualRows[virtualRows.length - 1].index : -1;
  const virtualColStart =
    virtualColumns.length > 0 ? virtualColumns[0].index : 0;
  const virtualColEnd =
    virtualColumns.length > 0
      ? virtualColumns[virtualColumns.length - 1].index
      : -1;

  // A merged region can be larger than the overscan window: its still-visible
  // portion must render even when its primary (top-left) cell scrolled out of
  // the virtual range. Render those primaries additionally, anchored at their
  // absolute offsets.
  const outOfRangePrimaries = mergedCells
    .getMergedCellsIntersecting(
      Math.max(snapRows, virtualRowStart),
      virtualRowEnd,
      Math.max(snapColumns, virtualColStart),
      virtualColEnd
    )
    .filter(
      (mergedCell: MergedCell) =>
        mergedCell.primaryCell.row >= snapRows &&
        mergedCell.primaryCell.col >= snapColumns &&
        !(
          mergedCell.primaryCell.row >= virtualRowStart &&
          mergedCell.primaryCell.row <= virtualRowEnd &&
          mergedCell.primaryCell.col >= virtualColStart &&
          mergedCell.primaryCell.col <= virtualColEnd
        )
    );

  return (
    // z-0 isolates cell-internal z-indexes so body overlays stay below the frozen panes
    <div className="absolute inset-0 z-0">
      {virtualRows.map((row) => {
        if (row.index < snapRows) {
          return null;
        }
        return virtualColumns.map((column) => {
          if (column.index < snapColumns) {
            return null;
          }

          const mergedCell = mergedCells.getCellMergeInfo(
            row.index,
            column.index
          );
          if (
            mergedCell &&
            !mergedCells.isPrimaryCellOfMergedRegion(row.index, column.index)
          ) {
            return null;
          }

          let style: React.CSSProperties = {
            top: row.start,
            left: column.start,
            width: columnWidths[column.index] ?? column.size,
            height: rowHeights[row.index] ?? row.size,
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
              key={`${row.index}-${column.index}`}
              row={row.index}
              col={column.index}
              style={style}
            />
          );
        });
      })}
      {outOfRangePrimaries.map((mergedCell) => (
        <Cell
          key={`${mergedCell.primaryCell.row}-${mergedCell.primaryCell.col}`}
          row={mergedCell.primaryCell.row}
          col={mergedCell.primaryCell.col}
          style={{
            top: sumTo(rowHeights, mergedCell.primaryCell.row),
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

export default VirtualGrid;
