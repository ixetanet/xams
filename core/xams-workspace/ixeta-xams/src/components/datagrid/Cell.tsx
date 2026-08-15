import React, { CSSProperties } from "react";
import useColor from "../../hooks/useColor";
import { Cell as CellType, CellLocation } from "./DataGridTypes";
import DataGridCellError from "./DataGridCellError";
import { useGridContext } from "./engine/GridContext";

interface EditInputProps {
  rightAlign: boolean;
  cell: CellType;
  cellLocation: CellLocation;
}

const EditInput = (props: EditInputProps) => {
  const gridContext = useGridContext();

  const onChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value;
    if (props.cell.onEditing != null) {
      value = props.cell.onEditing(value, props.cellLocation, props.cell.data);
    }
    gridContext.setEditValue(value);
  };

  return (
    <div className="w-full">
      <input
        type="text"
        style={{
          zIndex:
            gridContext.props.zIndex != null ? gridContext.props.zIndex + 1 : 2,
          textAlign: props.rightAlign ? "right" : "left",
        }}
        className={`w-full h-full border-none outline-none`}
        value={gridContext.editValue}
        onChange={onChange}
        autoFocus
      />
    </div>
  );
};

interface CellProps {
  row: number;
  col: number;
  style: CSSProperties;
}

const Cell = (props: CellProps) => {
  const gridContext = useGridContext();
  const color = useColor();

  const cell = gridContext.rows[props.row]?.columns[props.col];
  if (cell == null) {
    return null;
  }

  const cellLocation: CellLocation = { row: props.row, col: props.col };
  const cellValue = cell.value ?? "";
  const cellValueString = cellValue == null ? "" : cellValue.toString();

  const disabledColor =
    color.colorScheme === "light" ? "bg-gray-200" : "bg-neutral-800";
  const rangeColor =
    color.colorScheme === "light" ? "bg-blue-50" : "bg-blue-950";

  const isNumeric = (str: string) => {
    if (typeof str != "string") return false;
    return !isNaN(str as any) && !isNaN(parseFloat(str));
  };

  const rightAlignCell =
    gridContext.props.rightAlignNumbers && isNumeric(cellValueString);

  const isActiveCell =
    gridContext.activeCellLocation?.row === props.row &&
    gridContext.activeCellLocation?.col === props.col;

  const isInRange = gridContext.isCellInRange(props.row, props.col);
  const rangeEdges = gridContext.getRangeEdges(props.row, props.col);
  const fillPreviewEdges = gridContext.fill.getFillPreviewEdges(
    props.row,
    props.col
  );
  const copiedEdges = gridContext.copy.getCopiedRangeEdges(props.row, props.col);

  // The handle sits on the cell whose rendered rect owns the selection's
  // bottom-right corner (merged-cell aware via getRangeEdges), or on the
  // active cell when there is no range
  const showFillHandle =
    gridContext.fill.handleVisible &&
    (gridContext.selectedRange != null
      ? rangeEdges != null && rangeEdges.bottom && rangeEdges.right
      : isActiveCell);

  const onColumnResizeStart = (
    e: React.MouseEvent<HTMLDivElement, MouseEvent>
  ) => {
    e.preventDefault();
    gridContext.resizing.startColumnResize(props.col, e.clientX);
  };

  const onRowResizeStart = (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
    e.preventDefault();
    gridContext.resizing.startRowResize(props.row, e.clientY);
  };

  return (
    <div
      style={{
        position: "absolute",
        ...props.style,
        borderRight: `1px solid gray`,
        borderBottom: `1px solid gray`,
        ...(cell.style != null ? cell.style : {}),
      }}
      className={`box-border ${
        cell.custom == null ? `py-0.5` : ``
      } relative whitespace-nowrap text-ellipsis text-sm flex items-center ${
        cell.isDisabled ? disabledColor : isInRange ? rangeColor : ``
      } ${rightAlignCell ? `justify-end` : ``} ${
        // cells otherwise override the body-level crosshair mid-fill-drag
        gridContext.props.editable === false || gridContext.fill.isFillDragging
          ? ``
          : `cursor-pointer`
      }`}
      onMouseDown={(e) => {
        // shift-click is a range gesture; stop it extending native text selection
        if (e.shiftKey) {
          e.preventDefault();
        }
      }}
      onClick={(e) => {
        // The release ending a fill drag synthesizes a click; letting it
        // through would clear the selection the fill just committed
        if (gridContext.fill.shouldSuppressClick()) return;
        gridContext.onCellClick(cellLocation, e.shiftKey);
      }}
      onDoubleClick={() =>
        gridContext.onKeyDown(window, undefined, cellValueString)
      }
    >
      {rangeEdges != null && (
        <div
          style={{
            pointerEvents: "none",
            zIndex: gridContext.props.zIndex ?? 1,
            // border-{side}-2 utilities aren't in the compiled stylesheet, and
            // without preflight the widths must be explicit on every side
            borderTopWidth: rangeEdges.top ? 2 : 0,
            borderRightWidth: rangeEdges.right ? 2 : 0,
            borderBottomWidth: rangeEdges.bottom ? 2 : 0,
            borderLeftWidth: rangeEdges.left ? 2 : 0,
          }}
          className="absolute inset-0 box-border border-solid border-blue-600"
        ></div>
      )}
      {fillPreviewEdges != null && (
        <div
          style={{
            pointerEvents: "none",
            zIndex: gridContext.props.zIndex ?? 1,
            // border-dashed isn't in the compiled stylesheet, and the widths
            // must be explicit on every side, same as the range perimeter
            borderStyle: "dashed",
            borderTopWidth: fillPreviewEdges.top ? 2 : 0,
            borderRightWidth: fillPreviewEdges.right ? 2 : 0,
            borderBottomWidth: fillPreviewEdges.bottom ? 2 : 0,
            borderLeftWidth: fillPreviewEdges.left ? 2 : 0,
          }}
          className="absolute inset-0 box-border border-blue-600"
        ></div>
      )}
      {copiedEdges != null && (
        <>
          {/* Solid gap layer under the dashes — without it the dashed copy
              border is invisible wherever it overlaps the solid selection
              perimeter, which is exactly where it sits right after a copy */}
          <div
            style={{
              pointerEvents: "none",
              zIndex: gridContext.props.zIndex ?? 1,
              borderStyle: "solid",
              borderColor:
                color.colorScheme === "light" ? "#ffffff" : "#262626",
              borderTopWidth: copiedEdges.top ? 2 : 0,
              borderRightWidth: copiedEdges.right ? 2 : 0,
              borderBottomWidth: copiedEdges.bottom ? 2 : 0,
              borderLeftWidth: copiedEdges.left ? 2 : 0,
            }}
            className="absolute inset-0 box-border"
          ></div>
          <div
            style={{
              pointerEvents: "none",
              zIndex: gridContext.props.zIndex ?? 1,
              // border-dashed isn't in the compiled stylesheet, and the
              // widths must be explicit on every side
              borderStyle: "dashed",
              borderTopWidth: copiedEdges.top ? 2 : 0,
              borderRightWidth: copiedEdges.right ? 2 : 0,
              borderBottomWidth: copiedEdges.bottom ? 2 : 0,
              borderLeftWidth: copiedEdges.left ? 2 : 0,
            }}
            className="absolute inset-0 box-border border-blue-600"
          ></div>
        </>
      )}
      {!isActiveCell && cell.errorMessage != null && (
        <DataGridCellError
          isEditing={isActiveCell && gridContext.isEditing}
          errorMessage={cell.errorMessage}
        />
      )}
      <div
        onMouseDown={onColumnResizeStart}
        style={{
          zIndex: (gridContext.props.zIndex ?? 0) + 2,
        }}
        className="absolute -right-1 top-0 w-1.5 h-full cursor-ew-resize"
      ></div>
      {gridContext.props.resizableRows === true && (
        <div
          onMouseDown={onRowResizeStart}
          style={{
            zIndex: (gridContext.props.zIndex ?? 0) + 2,
          }}
          className="absolute -bottom-1 left-0 h-1.5 w-full cursor-row-resize"
        ></div>
      )}
      {showFillHandle && (
        <div
          onMouseDown={(e) => gridContext.fill.startFillDrag(e)}
          style={{
            // bg-blue-600, cursor-crosshair and the fixed w/h utilities
            // aren't in the compiled stylesheet, so the handle is styled
            // inline; +3 sits above the resize grips it overlaps
            position: "absolute",
            right: -4,
            bottom: -4,
            width: 7,
            height: 7,
            backgroundColor: "#2563eb",
            border: "1px solid #fff",
            cursor: "crosshair",
            zIndex: (gridContext.props.zIndex ?? 0) + 3,
          }}
        ></div>
      )}
      {isActiveCell ? (
        <div
          style={{
            pointerEvents: "none",
            boxSizing: gridContext.isEditing ? "content-box" : "border-box",
            zIndex: gridContext.props.zIndex ?? 1,
          }}
          className={`absolute ${
            gridContext.isEditing
              ? `-top-0.5 -left-0.5 border-blue-400 border-2`
              : `top-0 left-0 border-blue-600 border-2`
          }  w-full h-full border-solid`}
        >
          {cell.errorMessage != null && (
            <div className="w-full h-full absolute">
              <DataGridCellError
                isEditing={isActiveCell && gridContext.isEditing}
                errorMessage={cell.errorMessage}
              />
            </div>
          )}
          <div
            className={`w-full h-full ${
              gridContext.isEditing
                ? `border-2 border-solid border-blue-600 flex items-center`
                : ``
            } `}
          >
            {gridContext.isEditing && (
              <EditInput
                rightAlign={rightAlignCell ?? false}
                cell={cell}
                cellLocation={cellLocation}
              />
            )}
          </div>
        </div>
      ) : (
        ""
      )}
      {cell.custom != null
        ? cell.custom(cellValue, cellLocation, cell.data)
        : ""}
      {cell.custom == null && (
        <span className=" overflow-hidden px-1">
          {cellValueString.replaceAll(" ", "") === ""
            ? "\u00A0"
            : cellValueString}
        </span>
      )}
    </div>
  );
};

export default Cell;
