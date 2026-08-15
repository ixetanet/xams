import { useEffect, useRef } from "react";
import { CellLocation, DataGridProps } from "../DataGridTypes";

interface usePasteProps {
  props: DataGridProps;
  activeCell: CellLocation | undefined;
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
  const { props, activeCell, isEditing } = params;

  const onPaste = (e: ClipboardEvent) => {
    if (props.onPaste == null) return;
    if (props.editable === false) return;
    // While editing, the native edit input receives the paste
    if (activeCell == null || isEditing) return;
    const anchor = props.rows[activeCell.row]?.columns[activeCell.col];
    if (anchor == null) return;
    // Pasting onto a read-only/disabled cell pastes nothing at all
    if (anchor.isReadOnly || anchor.isDisabled) return;
    const text = e.clipboardData?.getData("text/plain");
    if (text == null || text === "") return;
    e.preventDefault();

    const matrix = parseClipboardText(text);
    const data: any[][] = [];
    const value: (string | undefined)[][] = [];
    for (let r = 0; r < matrix.length; r++) {
      const gridRow = props.rows[activeCell.row + r];
      if (gridRow == null) break; // clip rows past the end of the grid
      const dataRow: any[] = [];
      const valueRow: (string | undefined)[] = [];
      for (let c = 0; c < matrix[r].length; c++) {
        const cell = gridRow.columns[activeCell.col + c];
        if (cell == null) break; // clip columns past the end of the grid
        if (cell.isReadOnly || cell.isDisabled) {
          // Overlapping a read-only/disabled cell pastes nothing in that cell
          dataRow.push(undefined);
          valueRow.push(undefined);
        } else {
          dataRow.push(cell.data);
          valueRow.push(matrix[r][c]);
        }
      }
      data.push(dataRow);
      value.push(valueRow);
    }
    props.onPaste(data, value, activeCell);
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
