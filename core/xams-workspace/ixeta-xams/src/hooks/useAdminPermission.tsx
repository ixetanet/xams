import React, { useState } from "react";
import { CellLocation, Row } from "../components/datagrid/DataGridTypes";
import PermissionIcon from "../admin/PermissionIcon";
import { RolePermissionState } from "../admin/types/RolePermissionTypes";

interface useAdminPermissionProps {
  roleId: string;
  state: RolePermissionState;
  setState: React.Dispatch<React.SetStateAction<RolePermissionState>>;
}

export type PermissionLevels = "none" | "user" | "team" | "system";

const useAdminPermission = (props: useAdminPermissionProps) => {
  const [rows, setRows] = useState<Row[]>([]);

  const onClickBinaryPermission = (
    cellLocation: CellLocation,
    permissionName: string,
    rolePermissionId: string,
    hasPermission: boolean
  ) => {
    const newHasPermission = !hasPermission;

    // Doesn't exist but we want to create it
    if (rolePermissionId === "" && newHasPermission) {
      const newRolePermission = {
        RoleId: props.roleId,
        PermissionId: props.state.allPermissions.find(
          (p) => p.Name === permissionName
        )?.PermissionId as string,
        Permission: `permission.${permissionName}`,
      };
      props.setState((prevState) => {
        return {
          ...prevState,
          createRolePermissions: [
            ...prevState.createRolePermissions,
            newRolePermission,
          ],
          deleteRolePermissions: prevState.deleteRolePermissions.filter(
            (p) => p.Permission !== `permission.${permissionName}`
          ),
        };
      });
    }
    // If it doesn't exist and we no longer want to create it
    else if (rolePermissionId === "" && !newHasPermission) {
      props.setState((prevState) => {
        return {
          ...prevState,
          createRolePermissions: prevState.createRolePermissions.filter(
            (p) => p.Permission !== `permission.${permissionName}`
          ),
        };
      });
    }
    // If it exists and we want to delete it
    else if (rolePermissionId !== "" && !newHasPermission) {
      props.setState((prevState) => {
        return {
          ...prevState,
          createRolePermissions: prevState.createRolePermissions.filter(
            (p) =>
              p.PermissionId !== rolePermissionId ||
              p.Permission !== `permission.${permissionName}`
          ),
          deleteRolePermissions: [
            ...prevState.deleteRolePermissions,
            {
              RolePermissionId: rolePermissionId,
            },
          ],
        };
      });
    }
    // if it exists and we want to keep it
    else if (rolePermissionId !== "" && newHasPermission) {
      props.setState((prevState) => {
        return {
          ...prevState,
          createRolePermissions: prevState.createRolePermissions.filter(
            (p) =>
              p.PermissionId !== rolePermissionId ||
              p.Permission !== `permission.${permissionName}`
          ),
          deleteRolePermissions: prevState.deleteRolePermissions.filter(
            (p) => p.RolePermissionId !== rolePermissionId
          ),
        };
      });
    }

    setRows((prevRows) => {
      const newRows = [...prevRows];
      const row = newRows[cellLocation.row];
      const cell = row.columns[cellLocation.col];
      const newCell = { ...cell };
      newCell.custom = (value: string, cellLocation: CellLocation) => (
        <PermissionIcon
          icon={newHasPermission ? `system` : `none`}
          onClick={() =>
            onClickBinaryPermission(
              cellLocation,
              permissionName,
              rolePermissionId,
              newHasPermission
            )
          }
        ></PermissionIcon>
      );
      row.columns[cellLocation.col] = newCell;
      return newRows;
    });
  };

  const onClickTablePermission = (
    permissionName: string,
    cellLocation: CellLocation,
    rolePermissionId: string | null | undefined,
    tableName: string,
    operation: string
  ) => {
    let newPermissionLevel = "none" as PermissionLevels;
    let newPermissionName = permissionName;
    if (permissionName.endsWith("NONE")) {
      newPermissionName = newPermissionName
        .substring(0, permissionName.length - 4)
        .concat("USER");
      newPermissionLevel = "user";
    } else if (permissionName.endsWith("USER")) {
      newPermissionName = newPermissionName
        .substring(0, permissionName.length - 4)
        .concat("TEAM");
      newPermissionLevel = "team";
    } else if (permissionName.endsWith("TEAM")) {
      newPermissionName = newPermissionName
        .substring(0, permissionName.length - 4)
        .concat("SYSTEM");
      newPermissionLevel = "system";
    } else if (permissionName.endsWith("SYSTEM")) {
      newPermissionName = newPermissionName
        .substring(0, permissionName.length - 6)
        .concat("NONE");
      newPermissionLevel = "none";
    }
    // If the permission exists and we don't want it to then delete
    if (newPermissionLevel === "none" && rolePermissionId != null) {
      props.setState((prev) => {
        return {
          ...prev,
          deleteRolePermissions: [
            ...prev.deleteRolePermissions,
            {
              RolePermissionId: rolePermissionId,
            },
          ],
          // Remove it from update and create
          updateRolePermissions: prev.updateRolePermissions.filter(
            (x) => x.RolePermissionId !== rolePermissionId
          ),
          createRolePermissions: prev.createRolePermissions.filter(
            (x) => x.PermissionId !== rolePermissionId
          ),
        };
      });
    }
    // If the permission doesn't exist and we want it to then create
    if (newPermissionLevel !== "none" && rolePermissionId == null) {
      props.setState((prev) => {
        return {
          ...prev,
          createRolePermissions: [
            ...prev.createRolePermissions.filter(
              (x) => x.Permission !== `${tableName}.${operation}`
            ),
            {
              RolePermissionId: "",
              RoleId: props.roleId,
              PermissionId: props.state.allPermissions.find(
                (x) => x.Name === newPermissionName
              )?.PermissionId,
              Permission: `${tableName}.${operation}`,
            },
          ],
        };
      });
    }
    // If the permission exists and we want to change it then update
    if (newPermissionLevel !== "none" && rolePermissionId != null) {
      props.setState((prev) => {
        return {
          ...prev,
          updateRolePermissions: [
            ...prev.updateRolePermissions.filter(
              (x) => x.RolePermissionId !== rolePermissionId
            ),
            {
              RolePermissionId: rolePermissionId,
              PermissionId: props.state.allPermissions.find(
                (x) => x.Name === newPermissionName
              )?.PermissionId,
            },
          ],
          // remove it from deleteRolePermissions if it exists
          deleteRolePermissions: prev.deleteRolePermissions.filter(
            (x) => x.RolePermissionId !== rolePermissionId
          ),
        };
      });
    }

    setRows((prev) => {
      const newRows = prev.map((row, rowIndex) => {
        return {
          columns: row.columns.map((col, colIndex) => {
            if (
              rowIndex === cellLocation.row &&
              colIndex === cellLocation.col
            ) {
              return {
                ...col,
                custom: (value: string, cellLocation: CellLocation) => (
                  <PermissionIcon
                    icon={newPermissionLevel}
                    onClick={() =>
                      onClickTablePermission(
                        newPermissionName,
                        cellLocation,
                        rolePermissionId,
                        tableName,
                        operation
                      )
                    }
                  ></PermissionIcon>
                ),
              };
            }
            return col;
          }),
        };
      });
      return newRows;
    });
  };
  return {
    toggleBinaryPermission: onClickBinaryPermission,
    toggleTablePermission: onClickTablePermission,
    rows,
    setRows,
  };
};

export default useAdminPermission;
