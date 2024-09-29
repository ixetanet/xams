import { Tooltip } from "@mantine/core";
import React from "react";

interface DataGridCellErrorProps {
  isEditing: boolean;
  errorMessage?: string;
}

const DataGridCellError = (props: DataGridCellErrorProps) => {
  if (
    !props.isEditing &&
    props.errorMessage != null &&
    props.errorMessage !== ""
  ) {
    return (
      <Tooltip label={props.errorMessage} color="#dc2626" withArrow>
        <div className="absolute w-full h-full flex justify-start items-center">
          <div className="w-4 h-4 text-xs text-white rounded-full bg-red-600 flex justify-center items-center font-bold ml-1">
            !
          </div>
        </div>
      </Tooltip>
    );
  }
  return <></>;
};

export default DataGridCellError;
