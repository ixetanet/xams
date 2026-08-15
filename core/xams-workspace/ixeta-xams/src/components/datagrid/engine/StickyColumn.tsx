import React from "react";
import Cell from "../Cell";
import { useGridContext } from "./GridContext";

const sumTo = (values: number[], to: number) => {
  let total = 0;
  for (let i = 0; i < to; i++) {
    total += values[i] ?? 0;
  }
  return total;
};

// Frozen left columns. A zero-size sticky wrapper keeps the cells pinned
// horizontally while they scroll vertically with the body.
const StickyColumn = () => {
  const gridContext = useGridContext();
  const { snapRows, snapColumns, columnWidths, rowHeights, mergedCells } =
    gridContext;
  const { virtualRows } = gridContext.virtualized;

  if (snapColumns === 0) {
    return null;
  }

  const snapColumnOffsets: number[] = [];
  let offset = 0;
  for (let c = 0; c < snapColumns; c++) {
    snapColumnOffsets.push(offset);
    offset += columnWidths[c] ?? 0;
  }

  const virtualRowStart = virtualRows.length > 0 ? virtualRows[0].index : 0;
  const virtualRowEnd =
    virtualRows.length > 0 ? virtualRows[virtualRows.length - 1].index : -1;

  // Tall frozen-column merges can outgrow the row overscan: render primaries
  // whose row scrolled out of the virtual window so the visible part still
  // paints
  const outOfRangePrimaries = mergedCells
    .getMergedCellsIntersecting(
      Math.max(snapRows, virtualRowStart),
      virtualRowEnd,
      0,
      snapColumns - 1
    )
    .filter(
      (mergedCell) =>
        mergedCell.primaryCell.col < snapColumns &&
        mergedCell.primaryCell.row >= snapRows &&
        !(
          mergedCell.primaryCell.row >= virtualRowStart &&
          mergedCell.primaryCell.row <= virtualRowEnd
        )
    );

  return (
    <div className="sticky left-0 z-20" style={{ width: 0, height: 0 }}>
      {virtualRows.map((row) => {
        if (row.index < snapRows) {
          return null;
        }
        return Array.from({ length: snapColumns }, (_, colIndex) => {
          const mergedCell = mergedCells.getCellMergeInfo(row.index, colIndex);
          if (
            mergedCell &&
            !mergedCells.isPrimaryCellOfMergedRegion(row.index, colIndex)
          ) {
            return null;
          }

          let style: React.CSSProperties = {
            top: row.start,
            left: snapColumnOffsets[colIndex],
            width: columnWidths[colIndex] ?? 0,
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
              key={`${row.index}-${colIndex}`}
              row={row.index}
              col={colIndex}
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
            left: snapColumnOffsets[mergedCell.primaryCell.col],
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

export default StickyColumn;
