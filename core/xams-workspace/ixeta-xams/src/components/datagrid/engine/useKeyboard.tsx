import React, { useEffect, useRef } from "react";
import { CellLocation, DataGridProps } from "../DataGridTypes";
import { useMergedCellsType } from "./useMergedCells";

interface useKeyboardProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
  setActiveCell: React.Dispatch<React.SetStateAction<CellLocation | undefined>>;
  isEditing: boolean;
  setIsEditing: React.Dispatch<React.SetStateAction<boolean>>;
  editValue: string;
  setEditValue: React.Dispatch<React.SetStateAction<string>>;
  mergedCells: useMergedCellsType;
  columnWidths: number[];
  rowHeights: number[];
  snapRows: number;
  snapColumns: number;
  scrollRef: React.RefObject<HTMLDivElement | null>;
}

const useKeyboard = (params: useKeyboardProps) => {
  const { props, activeCell, isEditing, editValue, mergedCells } = params;
  const shiftKeyPressed = useRef(false);
  const ctrlPressed = useRef(false);

  const scrollCellIntoView = (cellLocation: CellLocation) => {
    const el = params.scrollRef.current;
    if (el == null) return;

    const sum = (values: number[], from: number, to: number) => {
      let total = 0;
      for (let i = from; i < to; i++) {
        total += values[i] ?? 0;
      }
      return total;
    };

    const top = sum(params.rowHeights, 0, cellLocation.row);
    const bottom = top + (params.rowHeights[cellLocation.row] ?? 0);
    const left = sum(params.columnWidths, 0, cellLocation.col);
    const right = left + (params.columnWidths[cellLocation.col] ?? 0);
    const snapHeight = sum(params.rowHeights, 0, params.snapRows);
    const snapWidth = sum(params.columnWidths, 0, params.snapColumns);

    let scrollTop = el.scrollTop;
    let scrollLeft = el.scrollLeft;
    if (cellLocation.row >= params.snapRows) {
      if (top < scrollTop + snapHeight) {
        scrollTop = top - snapHeight;
      } else if (bottom > scrollTop + el.clientHeight) {
        scrollTop = bottom - el.clientHeight;
      }
    }
    if (cellLocation.col >= params.snapColumns) {
      if (left < scrollLeft + snapWidth) {
        scrollLeft = left - snapWidth;
      } else if (right > scrollLeft + el.clientWidth) {
        scrollLeft = right - el.clientWidth;
      }
    }
    if (scrollTop !== el.scrollTop || scrollLeft !== el.scrollLeft) {
      el.scrollTo({ top: scrollTop, left: scrollLeft });
    }
  };

  // Moving onto a merged region lands on its primary cell
  const moveActiveCell = (row: number, col: number) => {
    const merged = mergedCells.getCellMergeInfo(row, col);
    const target = merged ? { ...merged.primaryCell } : { row, col };
    params.setActiveCell(target);
    scrollCellIntoView(target);
  };

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
      const cell = props.rows[activeCell.row]?.columns[activeCell.col];
      if (cell == null) return;
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
      // When the active cell is merged, navigate from the region's edges
      const merged = mergedCells.getCellMergeInfo(activeCell.row, activeCell.col);
      if (e.key === "ArrowDown") {
        const edgeRow = merged ? merged.end.row : activeCell.row;
        if (edgeRow === props.rows.length - 1) return;
        onEndEdit();
        moveActiveCell(edgeRow + 1, activeCell.col);
        keyPressed = true;
      } else if (e.key === "ArrowUp") {
        const edgeRow = merged ? merged.start.row : activeCell.row;
        if (edgeRow === 0) return;
        onEndEdit();
        moveActiveCell(edgeRow - 1, activeCell.col);
        keyPressed = true;
      } else if (e.key === "ArrowLeft") {
        const edgeCol = merged ? merged.start.col : activeCell.col;
        if (edgeCol === 0) return;
        onEndEdit();
        moveActiveCell(activeCell.row, edgeCol - 1);
        keyPressed = true;
      } else if (e.key === "ArrowRight") {
        const edgeCol = merged ? merged.end.col : activeCell.col;
        if (edgeCol === props.rows[0].columns.length - 1) return;
        onEndEdit();
        moveActiveCell(activeCell.row, edgeCol + 1);
        keyPressed = true;
      } else if (e.key === "Enter") {
        onEndEdit();
        const edgeRow = merged ? merged.end.row : activeCell.row;
        if (edgeRow === props.rows.length - 1) return;
        moveActiveCell(edgeRow + 1, activeCell.col);
      } else if (e.key === "Escape") {
        onCancelEdit();
      } else if (e.key === "Tab") {
        onEndEdit();
        if (shiftKeyPressed.current) {
          const edgeCol = merged ? merged.start.col : activeCell.col;
          if (edgeCol === 0) return;
          moveActiveCell(activeCell.row, edgeCol - 1);
        } else {
          const edgeCol = merged ? merged.end.col : activeCell.col;
          if (edgeCol === props.rows[0].columns.length - 1) return;
          moveActiveCell(activeCell.row, edgeCol + 1);
        }
        e.stopPropagation();
      } else {
        // e.metaKey covers Cmd shortcuts on macOS (e.g. Cmd+V paste), which
        // must not start an edit — the ctrlPressed ref only tracks Control
        if (!ctrlPressed.current && !e.ctrlKey && !e.metaKey) {
          onStartEdit(e);
        }
      }
    }
  };

  const onStartEdit = (e?: KeyboardEvent, value?: string) => {
    if (!isEditing && activeCell) {
      const cell = props.rows[activeCell.row]?.columns[activeCell.col];
      if (cell == null) return;
      if (cell.isReadOnly || cell.isDisabled) {
        return;
      }
      if (e == null) {
        params.setEditValue(value ?? "");
        params.setIsEditing(true);
      }
      if (e != null && e.key.length === 1) {
        params.setEditValue("");
        params.setIsEditing(true);
      }
    }
    keyPressed = false;
  };

  const onEndEdit = () => {
    if (activeCell && isEditing) {
      const cell = props.rows[activeCell.row]?.columns[activeCell.col];
      if (props.onEndEdit != null) {
        props.onEndEdit(editValue, activeCell, cell?.data);
      }
      if (cell?.onEndEdit != null) {
        cell.onEndEdit(editValue, activeCell, cell.data);
      }
      params.setEditValue("");
      params.setIsEditing(false);
    }
  };

  const onCancelEdit = () => {
    params.setEditValue("");
    params.setIsEditing(false);
  };

  const onKeyDownRef = useRef(onKeyDown);
  onKeyDownRef.current = onKeyDown;

  useEffect(() => {
    const keyDownEvent = (e: KeyboardEvent) => {
      onKeyDownRef.current(window, e);
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
  }, []);

  return {
    onKeyDown,
    onEndEdit,
  };
};

export default useKeyboard;
export type useKeyboardType = ReturnType<typeof useKeyboard>;
