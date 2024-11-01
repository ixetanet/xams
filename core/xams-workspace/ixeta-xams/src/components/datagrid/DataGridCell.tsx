import React, { useRef } from "react";
import { useDataGridContext } from "./DataGridContext";
import { useSubGridContext } from "./SubGridContext";
import TextInputCell from "./TextInputCell";
import useColor from "../../hooks/useColor";
import { Cell } from "./DataGridTypes";
import DataGridCellError from "./DataGridCellError";

interface DataGridCellProps {
  columnIndex: number;
  rowIndex: number;
  style: {};
}

const DataGridCell = (props: DataGridCellProps) => {
  const dgContext = useDataGridContext();
  const sgContext = useSubGridContext();
  const divRef = useRef<HTMLDivElement>(null);
  const color = useColor();

  if (dgContext.props.rows[props.rowIndex + sgContext.skipRows] == null) {
    return;
  }

  let cell = dgContext.props.rows[props.rowIndex + sgContext.skipRows].columns[
    props.columnIndex + sgContext.skipColumns
  ] as Cell;

  const cellLocation = {
    row: props.rowIndex + sgContext.skipRows,
    col: props.columnIndex + sgContext.skipColumns,
  };
  let cellValue = cell.value ?? "";
  let cellValueString = cellValue == null ? "" : cellValue.toString();

  const disabledColor =
    color.colorScheme === "light" ? "bg-gray-200" : "bg-neutral-800";

  const isNumeric = (str: string) => {
    if (typeof str != "string") return false;
    return !isNaN(str as any) && !isNaN(parseFloat(str));
  };

  const rightAlignCell =
    dgContext.props.rightAlignNumbers && isNumeric(cellValueString);

  const onCellClick = (row: number, col: number) => {
    const onClick =
      dgContext.props.rows[row + sgContext.skipRows].columns[
        col + sgContext.skipColumns
      ].onClick;
    if (onClick != null) {
      const data =
        dgContext.props.rows[row + sgContext.skipRows].columns[
          col + sgContext.skipColumns
        ].data;
      onClick(
        cellValue,
        {
          row: row + sgContext.skipRows,
          col: col + sgContext.skipColumns,
        },
        data
      );
    }

    if (dgContext.props.editable === false) {
      return;
    }
    dgContext.setActiveCellLocation({
      row: row + sgContext.skipRows,
      col: col + sgContext.skipColumns,
    });
    if (dgContext.isEditing) {
      dgContext.onEndEdit();
    }
  };

  let cellStyle =
    dgContext.props.rows[props.rowIndex + sgContext.skipRows].columns[
      props.columnIndex + sgContext.skipColumns
    ].style;

  const onWidthChange = (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
    dgContext.gridResize.setIsResizing(true);
    dgContext.gridResize.setResizingStartClientX(e.clientX);
    dgContext.gridResize.setResizingColNumber(
      props.columnIndex + sgContext.skipColumns
    );
  };

  const isActiveCell =
    dgContext.activeCellLocation?.row === props.rowIndex + sgContext.skipRows &&
    dgContext.activeCellLocation?.col ===
      props.columnIndex + sgContext.skipColumns;

  return (
    <>
      <div
        key={props.columnIndex}
        ref={divRef}
        style={{
          ...props.style,
          borderRight: `1px solid gray`,
          borderBottom: `1px solid gray`,
          ...(cellStyle != null ? cellStyle : {}),
        }}
        className={`h-full py-0.5 relative whitespace-nowrap text-ellipsis text-sm flex items-center ${
          cell.isDisabled ? disabledColor : ``
        } ${rightAlignCell ? `justify-end` : ``} ${
          dgContext.props.editable === false ? `` : `cursor-pointer`
        }`}
        onClick={() => onCellClick(props.rowIndex, props.columnIndex)}
        onDoubleClick={() =>
          dgContext.onKeyDown(window, undefined, cellValueString)
        }
      >
        {!isActiveCell && cell.errorMessage != null && (
          <DataGridCellError
            isEditing={isActiveCell && dgContext.isEditing}
            errorMessage={cell.errorMessage}
          />
        )}
        <div
          onMouseDown={(e) => onWidthChange(e)}
          style={{
            zIndex: (dgContext.props.zIndex ?? 0) + 2,
          }}
          className="absolute -right-1 top-0 w-1.5 h-full cursor-ew-resize"
        ></div>
        {isActiveCell ? (
          <div
            style={{
              boxSizing: dgContext.isEditing ? "content-box" : "border-box",
              zIndex: dgContext.props.zIndex ?? 1,
            }}
            className={`absolute ${
              dgContext.isEditing
                ? `-top-0.5 -left-0.5 border-blue-400 border-2`
                : `top-0 left-0 border-blue-600 border-1`
            }  w-full h-full border-solid`}
          >
            {cell.errorMessage != null && (
              <div className="w-full h-full absolute">
                <DataGridCellError
                  isEditing={isActiveCell && dgContext.isEditing}
                  errorMessage={cell.errorMessage}
                />
              </div>
            )}
            <div
              className={`w-full h-full ${
                dgContext.isEditing
                  ? `border-2 border-solid border-blue-600 flex items-center`
                  : ``
              } `}
            >
              {dgContext.isEditing && (
                <TextInputCell rightAlign={rightAlignCell ?? false} />
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
    </>
  );
};

export default DataGridCell;
