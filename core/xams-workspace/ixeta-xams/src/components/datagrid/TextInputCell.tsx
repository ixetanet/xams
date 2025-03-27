import React from "react";
import { useDataGridContext } from "./DataGridContext";
import { CellLocation } from "./DataGridTypes";

interface TextInputCellProps {
  rightAlign: boolean;
}

const TextInputCell = (props: TextInputCellProps) => {
  const dgContext = useDataGridContext();

  const onChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value;
    if (
      dgContext.activeCell &&
      dgContext.activeCell.onEditing &&
      dgContext.activeCellLocation
    ) {
      value = dgContext.activeCell.onEditing(
        value,
        dgContext.activeCellLocation,
        dgContext.activeCell?.data
      );
    }
    dgContext.setEditValue(value);
  };

  return (
    <div className="w-full">
      <input
        type="text"
        style={{
          zIndex:
            dgContext.props.zIndex != null ? dgContext.props.zIndex + 1 : 2,
          textAlign: props.rightAlign ? "right" : "left",
        }}
        className={`w-full h-full border-none outline-none`}
        value={dgContext.editValue}
        onChange={onChange}
        // onChange={(e) => {
        //   dgContext.setEditValue(e.target.value);
        // }}
        autoFocus
      />
    </div>
  );
};

export default TextInputCell;
