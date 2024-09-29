import { TablePermissions } from "../../stores/usePermissionStore";
import { Loader } from "@mantine/core";
import React from "react";
import { useDataTableContext } from "../DataTableImp";

interface DataTableOverlayProps {
  disabledMessage?: string;
  tableName: string;
  permissions: TablePermissions;
  isLoadingData?: boolean;
}

const DataTableOverlay = (props: DataTableOverlayProps) => {
  const ctx = useDataTableContext();
  if (props.disabledMessage !== undefined) {
    return (
      <div className="w-full h-full flex flex-col justify-center items-center">
        {props.disabledMessage}
      </div>
    );
  }

  if (props.isLoadingData) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  // if (props.permissions.read === PermissionLevel.NONE) {
  if (ctx.state.type === "MISSING_READ_PERMISSIONS") {
    return (
      <div className="w-full h-full flex flex-col justify-center items-center">
        <div>Missing read permission for {props.tableName}.</div>
        {/* <div className="flex justify-center">Permission:</div> */}
        {/* {readPermisions.split(",").map((p, i) => {
          return (
            <div key={i}>{p.replaceAll("{tableName}", props.tableName)}</div>
          );
        })} */}
      </div>
    );
  }
  return undefined;
};

export default DataTableOverlay;
