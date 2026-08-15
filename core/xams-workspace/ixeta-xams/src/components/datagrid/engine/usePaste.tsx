import { useEffect, useRef } from "react";
import { CellLocation, CellRange, DataGridProps } from "../DataGridTypes";

interface usePasteProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
  selectedRange: CellRange | null;
  isEditing: boolean;
}

// Excel and other spreadsheets put a tab-separated matrix on the clipboard,
// wrapping cells that contain tabs/newlines/quotes in double quotes with
// embedded quotes doubled. An unterminated quote means the text was not
// actually quoted TSV (e.g. a lone cell starting with "), so fall back to a
// plain tab/newline split.
export const parseClipboardText = (text: string): string[][] => {
  const rows: string[][] = [];
  let row: string[] = [];
  let cell = "";
  let inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (inQuotes) {
      if (ch === '"') {
        if (text[i + 1] === '"') {
          cell += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        cell += ch;
      }
    } else if (ch === '"' && cell === "") {
      inQuotes = true;
    } else if (ch === "\t") {
      row.push(cell);
      cell = "";
    } else if (ch === "\r" || ch === "\n") {
      if (ch === "\r" && text[i + 1] === "\n") {
        i++;
      }
      row.push(cell);
      cell = "";
      rows.push(row);
      row = [];
    } else {
      cell += ch;
    }
  }
  if (inQuotes) {
    const lines = text.split(/\r\n|\r|\n/);
    if (lines[lines.length - 1] === "") {
      lines.pop();
    }
    return lines.map((line) => line.split("\t"));
  }
  // Excel appends a trailing newline — don't emit an empty row for it
  if (cell !== "" || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }
  return rows;
};

const usePaste = (params: usePasteProps) => {
  const { props, activeCell, selectedRange, isEditing } = params;

  const onPaste = (e: ClipboardEvent) => {
    if (props.onPaste == null) return;
    if (props.editable === false) return;
    // While editing, the native edit input receives the paste
    if (activeCell == null || isEditing) return;
    // Paste anchors at the top-left of the selection (or the active cell
    // when nothing is range-selected), like other spreadsheets. The range
    // is normalized here — a shift-click drag can build it in any direction.
    const selection = selectedRange ?? { start: activeCell, end: activeCell };
    const anchor: CellLocation = {
      row: Math.min(selection.start.row, selection.end.row),
      col: Math.min(selection.start.col, selection.end.col),
    };
    const anchorCell = props.rows[anchor.row]?.columns[anchor.col];
    if (anchorCell == null) return;
    // Pasting onto a read-only/disabled cell pastes nothing at all
    if (anchorCell.isReadOnly || anchorCell.isDisabled) return;
    const text = e.clipboardData?.getData("text/plain");
    if (text == null || text === "") return;
    e.preventDefault();

    const matrix = parseClipboardText(text);
    const matRows = matrix.length;
    const matCols = matrix.reduce((max, row) => Math.max(max, row.length), 0);
    if (matRows === 0 || matCols === 0) return;

    // Spreadsheet tiling: a selection that is an exact multiple of the
    // copied block in both dimensions is filled by repeating the block —
    // e.g. one copied cell pastes into every selected cell. Any other
    // selection gets the block once, anchored at the selection's top-left.
    const selRows = Math.abs(selection.end.row - selection.start.row) + 1;
    const selCols = Math.abs(selection.end.col - selection.start.col) + 1;
    let effective = matrix;
    if (
      (selRows > matRows || selCols > matCols) &&
      selRows % matRows === 0 &&
      selCols % matCols === 0
    ) {
      effective = Array.from({ length: selRows }, (_, r) =>
        Array.from(
          { length: selCols },
          (_, c) => matrix[r % matRows][c % matCols] ?? ""
        )
      );
    }

    const data: any[][] = [];
    const value: (string | undefined)[][] = [];
    for (let r = 0; r < effective.length; r++) {
      const gridRow = props.rows[anchor.row + r];
      if (gridRow == null) break; // clip rows past the end of the grid
      const dataRow: any[] = [];
      const valueRow: (string | undefined)[] = [];
      for (let c = 0; c < effective[r].length; c++) {
        const cell = gridRow.columns[anchor.col + c];
        if (cell == null) break; // clip columns past the end of the grid
        if (cell.isReadOnly || cell.isDisabled) {
          // Overlapping a read-only/disabled cell pastes nothing in that cell
          dataRow.push(undefined);
          valueRow.push(undefined);
        } else {
          dataRow.push(cell.data);
          valueRow.push(effective[r][c]);
        }
      }
      data.push(dataRow);
      value.push(valueRow);
    }
    props.onPaste(data, value, anchor);
  };

  const onPasteRef = useRef(onPaste);
  onPasteRef.current = onPaste;

  useEffect(() => {
    const pasteEvent = (e: ClipboardEvent) => {
      onPasteRef.current(e);
    };
    window.addEventListener("paste", pasteEvent);
    return () => {
      window.removeEventListener("paste", pasteEvent);
    };
  }, []);
};

export default usePaste;
