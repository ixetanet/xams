import React from "react";
import { useDataGridContext } from "./DataGridContext";

interface TextInputCellProps {
  rightAlign: boolean;
}

const TextInputCell = (props: TextInputCellProps) => {
  const dgContext = useDataGridContext();
  return (
    <div>
      <input
        type="text"
        style={{
          zIndex:
            dgContext.props.zIndex != null ? dgContext.props.zIndex + 1 : 2,
          textAlign: props.rightAlign ? "right" : "left",
        }}
        className={`w-full h-full border-none outline-none`}
        value={dgContext.editValue}
        onChange={(e) => {
          dgContext.setEditValue(e.target.value);
        }}
        autoFocus
      />
    </div>
  );
};

export default TextInputCell;
