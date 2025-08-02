import React, {
  Ref,
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
} from "react";
import AutoSizer from "react-virtualized-auto-sizer";
import { VariableSizeGrid as Grid, VariableSizeGrid } from "react-window";
import DataGridCell from "./datagrid/DataGridCell";
import { DataGridContext } from "./datagrid/DataGridContext";
import {
  CellLocation,
  Row,
  DataGridProps as DataGridProps,
} from "./datagrid/DataGridTypes";
import { GridSubContext } from "./datagrid/SubGridContext";
import useDataGridCellSize from "./datagrid/useDataGridCellSize";
import DataGridValidation from "./datagrid/DataGridValidation";

export interface DataGridRef {
  reset: () => void;
  activeCell?: CellLocation;
  setActiveCell: (cell: CellLocation) => void;
  isEditing?: boolean;
  editValue?: string;
  setEditValue: (value: string) => void;
}

const DataGrid = forwardRef((props: DataGridProps, ref: Ref<DataGridRef>) => {
  const [activeCell, setActiveCell] = useState<undefined | CellLocation>(
    undefined
  );
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState("");
  const shiftKeyPressed = useRef(false);
  const ctrlPressed = useRef(false);
  const divRef = useRef<HTMLDivElement>(null);

  const staticColumnStaticRows = useRef<VariableSizeGrid>(null);
  const staticColumns = useRef<VariableSizeGrid>(null);
  const staticRows = useRef<VariableSizeGrid>(null);
  const mainGrid = useRef<VariableSizeGrid>(null);

  // Validation
  DataGridValidation(props);

  const onScroll = useCallback(
    ({
      scrollLeft,
      scrollTop,
      scrollUpdateWasRequested,
    }: {
      scrollLeft: number;
      scrollTop: number;
      scrollUpdateWasRequested: boolean;
    }) => {
      if (!scrollUpdateWasRequested) {
        staticColumns.current?.scrollTo({ scrollLeft: 0, scrollTop });
        staticRows.current?.scrollTo({ scrollLeft: scrollLeft, scrollTop: 0 });
      }
    },
    []
  );

  const gridCellSize = useDataGridCellSize({
    rows: props.rows,
    divRef: divRef,
    columnWidths:
      typeof props.columnWidths === "number" && props.rows.length > 0
        ? Array(props.rows[0].columns.length).fill(props.columnWidths)
        : typeof props.columnWidths === "object"
        ? (props.columnWidths as number[])
        : [],
  });

  const columnCount =
    props.rows.length > 0 && props.rows[0].columns.length > 0
      ? props.rows[0].columns.length
      : 0;

  const snapColumnsWidth = gridCellSize.columnWidths
    .slice(0, props.snapColumns ?? 0)
    .reduce((acc, current) => acc + current, 0);

  let keyPressed = false;
  const onKeyDown = (w: Window, e?: KeyboardEvent, value?: string) => {
    if (props.editable === false) return;
    if (e?.key === "Shift") {
      shiftKeyPressed.current = true;
    }
    if (e?.key === "Control") {
      ctrlPressed.current = true;
    }
    if (keyPressed) return;
    if (e === undefined) {
      onStartEdit(e, value);
      return;
    }
    if (activeCell) {
      const cell = props.rows[activeCell.row].columns[activeCell.col];
      if (e.key === "Delete") {
        if (props.onDelete != null) {
          props.onDelete(editValue, activeCell, cell.data);
        }
        if (cell.onDelete != null) {
          cell.onDelete(editValue, activeCell, cell.data);
        }
      }
      if (e.key === "Backspace") {
        if (props.onBackspace != null) {
          props.onBackspace(editValue, activeCell, cell.data);
        }
        if (cell.onBackspace != null) {
          cell.onBackspace(editValue, activeCell, cell.data);
        }
      }
      if (
        e.key === "ArrowDown" ||
        e.key === "ArrowUp" ||
        e.key === "ArrowLeft" ||
        e.key === "ArrowRight" ||
        e.key === "Enter" ||
        e.key === "Escape" ||
        e.key === "Tab"
      ) {
        e.preventDefault();
      }
      if (e.key === "ArrowDown") {
        if (activeCell.row === props.rows.length - 1) return;
        onEndEdit();
        setActiveCell({
          row: activeCell.row + 1,
          col: activeCell.col,
        });
        keyPressed = true;
      } else if (e.key === "ArrowUp") {
        if (activeCell.row === 0) return;
        onEndEdit();
        setActiveCell({
          row: activeCell.row - 1,
          col: activeCell.col,
        });
        keyPressed = true;
      } else if (e.key === "ArrowLeft") {
        if (activeCell.col === 0) return;
        onEndEdit();
        setActiveCell({
          row: activeCell.row,
          col: activeCell.col - 1,
        });
        keyPressed = true;
      } else if (e.key === "ArrowRight") {
        if (activeCell.col === props.rows[0].columns.length - 1) return;
        onEndEdit();
        setActiveCell({
          row: activeCell.row,
          col: activeCell.col + 1,
        });
        keyPressed = true;
      } else if (e.key === "Enter") {
        onEndEdit();
        if (activeCell.row === props.rows.length - 1) return;
        setActiveCell({
          row: activeCell.row + 1,
          col: activeCell.col,
        });
      } else if (e.key === "Escape") {
        onCancelEdit();
      } else if (e.key === "Tab") {
        onEndEdit();
        if (shiftKeyPressed.current) {
          if (activeCell.col === 0) return;
          setActiveCell({
            row: activeCell.row,
            col: activeCell.col - 1,
          });
        } else {
          if (activeCell.col === props.rows[0].columns.length - 1) return;
          setActiveCell({
            row: activeCell.row,
            col: activeCell.col + 1,
          });
        }
        e.stopPropagation();
      } else {
        if (!ctrlPressed.current) {
          onStartEdit(e);
        }
      }
    }
  };

  const onStartEdit = (e?: KeyboardEvent, value?: string) => {
    if (!isEditing && activeCell) {
      let isReadOnly =
        props.rows[activeCell.row].columns[activeCell.col].isReadOnly;
      let isDisabled =
        props.rows[activeCell.row].columns[activeCell.col].isDisabled;
      if (isReadOnly || isDisabled) {
        return;
      }
      if (e == null) {
        setEditValue(value ?? "");
        setIsEditing(true);
      }
      if (e != null && e.key.length === 1) {
        setEditValue("");
        setIsEditing(true);
      }
    }
    keyPressed = false;
  };

  const onEndEdit = () => {
    if (activeCell && isEditing) {
      const cell = props.rows[activeCell.row].columns[activeCell.col];
      if (props.onEndEdit != null) {
        props.onEndEdit(editValue, activeCell, cell.data);
      }
      if (cell.onEndEdit != null) {
        cell.onEndEdit(editValue, activeCell, cell.data);
      }
      setEditValue("");
      setIsEditing(false);
    }
  };

  const onCancelEdit = () => {
    setEditValue("");
    setIsEditing(false);
  };

  useEffect(() => {
    const keyDownEvent = (e: KeyboardEvent) => {
      onKeyDown(window, e);
    };
    const keyUpEvent = (e: KeyboardEvent) => {
      if (e.key === "Shift") {
        shiftKeyPressed.current = false;
      }
      if (e.key === "Control") {
        ctrlPressed.current = false;
      }
    };

    window.addEventListener("keydown", keyDownEvent);
    window.addEventListener("keyup", keyUpEvent);

    return () => {
      window.removeEventListener("keydown", keyDownEvent);
      window.removeEventListener("keyup", keyUpEvent);
    };
  }, [activeCell, isEditing, editValue]);

  const handleClickOutside = (event: any) => {
    if (divRef.current && !divRef.current.contains(event.target)) {
      setActiveCell(undefined);
    }
  };

  const reset = () => {
    staticColumnStaticRows?.current?.resetAfterColumnIndex(0);
    staticColumns.current?.resetAfterColumnIndex(0);
    staticRows.current?.resetAfterColumnIndex(0);
    mainGrid.current?.resetAfterColumnIndex(0);
  };

  useImperativeHandle(ref, () => ({
    reset: () => {
      reset();
    },
    activeCell: activeCell,
    setActiveCell: (cell: CellLocation) => {
      setActiveCell(cell);
    },
    isEditing: isEditing,
    editValue: editValue,
    setEditValue: (value: string) => {
      setEditValue(value);
    },
  }));

  useEffect(() => {
    reset();
  }, [gridCellSize.columnWidths]);

  useEffect(() => {
    document.addEventListener("click", handleClickOutside, true);
    return () => {
      document.removeEventListener("click", handleClickOutside, true);
    };
  }, []);

  let colWidthsKey = "";
  if (props.columnWidths != null && typeof props.columnWidths === "object") {
    for (let i = 0; i < gridCellSize.columnWidths.length; i++) {
      colWidthsKey += gridCellSize.columnWidths[i];
    }
  } else {
    colWidthsKey = props.columnWidths.toString();
  }

  return (
    <div
      // make this a key so that the component is re-rendered when any of these props change
      key={`${props.snapColumns}-${props.snapRows}-${colWidthsKey}`}
      ref={divRef}
      className="w-full h-full relative"
      onMouseMove={(e) => gridCellSize.onMouseMove(e)}
    >
      {gridCellSize.isResizing && (
        <div
          style={{
            top: gridCellSize.mousePosition.startY,
            left: gridCellSize.mousePosition.startX,
            border: "1px solid black",
            width: 1,
            height: gridCellSize.mousePosition.endY,
            zIndex: props.zIndex == null ? 2 : props.zIndex + 2,
          }}
          className="absolute"
        ></div>
      )}
      <AutoSizer>
        {({ height, width }) => {
          return (
            <DataGridContext.Provider
              value={{
                props,
                activeCell:
                  activeCell != null &&
                  props.rows[activeCell.row] != null &&
                  props.rows[activeCell.row].columns[activeCell.col] != null
                    ? props.rows[activeCell.row].columns[activeCell.col]
                    : undefined,
                editValue,
                setEditValue,
                activeCellLocation: activeCell,
                setActiveCellLocation: setActiveCell,
                isEditing,
                setIsEditing,
                onKeyDown,
                onEndEdit,
                gridResize: gridCellSize,
              }}
            >
              <div
                style={{
                  display: "flex",
                  flexDirection: "row",
                  width: width,
                  height: height,
                }}
              >
                {props.snapColumns != null && props.snapColumns > 0 && (
                  <div
                    style={{
                      width: snapColumnsWidth,
                      height: `100%`,
                    }}
                  >
                    {props.snapRows != null && props.snapRows > 0 && (
                      <GridSubContext.Provider
                        value={{
                          skipColumns: 0,
                          skipRows: 0,
                        }}
                      >
                        <Grid
                          ref={staticColumnStaticRows}
                          columnCount={props.snapColumns}
                          rowCount={props.snapRows}
                          columnWidth={(index) =>
                            gridCellSize.columnWidths[index]
                          }
                          rowHeight={(index) => 30}
                          height={props.snapRows * 30}
                          width={snapColumnsWidth}
                          style={{
                            overflow: `hidden`,
                            ...props.styletl,
                          }}
                          className=" border border-solid border-gray-500"
                        >
                          {DataGridCell}
                        </Grid>
                      </GridSubContext.Provider>
                    )}

                    <GridSubContext.Provider
                      value={{
                        skipColumns: 0,
                        skipRows: props.snapRows ?? 0,
                      }}
                    >
                      <Grid
                        ref={staticColumns}
                        columnCount={props.snapColumns}
                        rowCount={props.rows.length - (props.snapRows ?? 0)}
                        columnWidth={(index) =>
                          gridCellSize.columnWidths[index]
                        }
                        rowHeight={(index) => 30}
                        height={height - (props.snapRows ?? 0) * 30}
                        width={snapColumnsWidth}
                        style={{
                          overflow: `hidden`,
                          ...props.stylebl,
                        }}
                        className=" border border-solid border-gray-500"
                      >
                        {DataGridCell}
                      </Grid>
                    </GridSubContext.Provider>
                  </div>
                )}

                <div className="w-full h-full flex flex-col">
                  {props.snapRows != null && props.snapRows > 0 && (
                    <GridSubContext.Provider
                      value={{
                        skipColumns: props.snapColumns ?? 0,
                        skipRows: 0,
                      }}
                    >
                      <Grid
                        ref={staticRows}
                        columnCount={columnCount - (props.snapColumns ?? 0)}
                        rowCount={props.snapRows}
                        columnWidth={(index) =>
                          gridCellSize.columnWidths[
                            index + (props.snapColumns ?? 0)
                          ]
                        }
                        rowHeight={(index) => 30}
                        height={props.snapRows * 30}
                        width={
                          props.snapColumns != null && props.snapColumns > 0
                            ? width - snapColumnsWidth
                            : width
                        }
                        style={{
                          overflow: `hidden`,
                          ...props.styletr,
                        }}
                        className=" border border-solid border-gray-500"
                      >
                        {DataGridCell}
                      </Grid>
                    </GridSubContext.Provider>
                  )}

                  <GridSubContext.Provider
                    value={{
                      skipColumns: props.snapColumns ?? 0,
                      skipRows: props.snapRows ?? 0,
                    }}
                  >
                    <Grid
                      ref={mainGrid}
                      onScroll={onScroll}
                      columnCount={columnCount - (props.snapColumns ?? 0)}
                      rowCount={props.rows.length - (props.snapRows ?? 0)}
                      columnWidth={(index) => {
                        console.log(props.snapColumns);
                        return gridCellSize.columnWidths[
                          index + (props.snapColumns ?? 0)
                        ];
                      }}
                      rowHeight={(index) => 30}
                      height={
                        props.snapRows != null && props.snapRows > 0
                          ? height - props.snapRows * 30
                          : height
                      }
                      width={
                        props.snapColumns != null && props.snapColumns > 0
                          ? width - snapColumnsWidth
                          : width
                      }
                      style={{
                        ...props.style,
                        // border: `1px solid`,
                      }}
                      className="thin-scrollbar-x border border-solid border-gray-500"
                    >
                      {DataGridCell}
                    </Grid>
                  </GridSubContext.Provider>
                </div>
              </div>
            </DataGridContext.Provider>
          );
        }}
      </AutoSizer>
    </div>
  );
});

DataGrid.displayName = "DataGrid";
export default DataGrid;
