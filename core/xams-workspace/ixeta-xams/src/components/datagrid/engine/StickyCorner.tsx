import React from "react";
import Cell from "../Cell";
import { useGridContext } from "./GridContext";

// Frozen top-left pane (snapRows × snapColumns). Its cells never scroll, so the
// wrapper is a real box that also carries the styletl pane chrome and clips
// its own content (rounded corners clip like the old pane did).
const StickyCorner = () => {
  const gridContext = useGridContext();
  const { rows, snapRows, snapColumns, columnWidths, rowHeights, mergedCells } =
    gridContext;

  if (snapRows === 0 || snapColumns === 0) {
    return null;
  }

  const snapRowOffsets: number[] = [];
  let snapHeight = 0;
  for (let r = 0; r < snapRows; r++) {
    snapRowOffsets.push(snapHeight);
    snapHeight += rowHeights[r] ?? 0;
  }
  const snapColumnOffsets: number[] = [];
  let snapWidth = 0;
  for (let c = 0; c < snapColumns; c++) {
    snapColumnOffsets.push(snapWidth);
    snapWidth += columnWidths[c] ?? 0;
  }

  return (
    <div
      className="sticky top-0 left-0 z-40 overflow-hidden border border-solid border-gray-500"
      style={{
        width: snapWidth,
        height: snapHeight,
        backgroundColor: "var(--mantine-color-body, transparent)",
        ...gridContext.props.styletl,
      }}
    >
      {Array.from({ length: snapRows }, (_, rowIndex) =>
        Array.from({ length: snapColumns }, (_, colIndex) => {
          if (rows[rowIndex] == null) {
            return null;
          }

          const mergedCell = mergedCells.getCellMergeInfo(rowIndex, colIndex);
          if (
            mergedCell &&
            !mergedCells.isPrimaryCellOfMergedRegion(rowIndex, colIndex)
          ) {
            return null;
          }

          let style: React.CSSProperties = {
            top: snapRowOffsets[rowIndex],
            left: snapColumnOffsets[colIndex],
            width: columnWidths[colIndex] ?? 0,
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
              key={`${rowIndex}-${colIndex}`}
              row={rowIndex}
              col={colIndex}
              style={style}
            />
          );
        })
      )}
    </div>
  );
};

export default StickyCorner;
