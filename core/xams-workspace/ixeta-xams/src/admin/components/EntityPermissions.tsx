import React, { useEffect } from "react";
import DataGrid from "../../components/DataGrid";
import { CellLocation, Row } from "../../components/datagrid/DataGridTypes";
import { Permission } from "../../api/Permission";
import { RolePermission } from "../../api/RolePermission";
import { TablesResponse } from "../../api/TablesResponse";
import PermissionIcon from "../PermissionIcon";
import {
  RolePermissionState,
  SystemAdministratorRoleId,
} from "../types/RolePermissionTypes";
import useAdminPermission, {
  PermissionLevels,
} from "../../hooks/useAdminPermission";

interface EntityPermissionsProps {
  roleId: string;
  state: RolePermissionState;
  setState: React.Dispatch<React.SetStateAction<RolePermissionState>>;
}

const EntityPermissions = (props: EntityPermissionsProps) => {
  const adminPermission = useAdminPermission(props);

  const findTablePermission = (
    rolePermissions: RolePermission[],
    tableName: string,
    permissionsName: string
  ): {
    permissionLevel: PermissionLevels;
    RolePermissionId: string | null | undefined;
  } => {
    let permissionLevel = "none" as PermissionLevels;
    const permission = rolePermissions.find((rp) =>
      rp.Permission?.startsWith(`TABLE_${tableName}_${permissionsName}`)
    );
    if (permission == null) {
      return {
        permissionLevel: "none",
        RolePermissionId: null,
      };
    }
    if (
      permission.Permission?.endsWith("IMPORT") ||
      permission.Permission?.endsWith("EXPORT")
    ) {
      permissionLevel = "system";
    }
    if (permission.Permission?.endsWith("USER")) {
      permissionLevel = "user";
    }
    if (permission.Permission?.endsWith("TEAM")) {
      permissionLevel = "team";
    }
    if (permission.Permission?.endsWith("SYSTEM")) {
      permissionLevel = "system";
    }
    return {
      permissionLevel,
      RolePermissionId: permission.RolePermissionId,
    };
  };

  const buildTable = (
    rolePermissionsResp: RolePermission[],
    tablesResp: TablesResponse[]
  ) => {
    const rows = [] as Row[];

    const row1 = {
      columns: [
        {
          value: "Entity",
          isReadOnly: true,
          style: {
            // justifyContent: "center",
          },
        },
        {
          value: "Create",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Read",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Update",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Delete",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Assign",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Import",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
        {
          value: "Export",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
      ],
    } as Row;
    rows.push(row1);

    for (let table of tablesResp as TablesResponse[]) {
      const createRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "CREATE"
      );
      const readRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "READ"
      );
      const updateRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "UPDATE"
      );
      const deleteRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "DELETE"
      );
      const assignRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "ASSIGN"
      );
      const importRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "IMPORT"
      );
      const exportRolePermission = findTablePermission(
        rolePermissionsResp,
        table.tableName,
        "EXPORT"
      );

      const canUpdate = props.roleId !== SystemAdministratorRoleId;

      const row = {
        columns: [
          {
            value: table.tableName,
            isReadOnly: true,
            style: {
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={createRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleTablePermission(
                    `TABLE_${
                      table.tableName
                    }_CREATE_${createRolePermission.permissionLevel.toUpperCase()}`,
                    cellLocation,
                    createRolePermission.RolePermissionId,
                    table.tableName,
                    "CREATE"
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={readRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleTablePermission(
                    `TABLE_${
                      table.tableName
                    }_READ_${readRolePermission.permissionLevel.toUpperCase()}`,
                    cellLocation,
                    readRolePermission.RolePermissionId,
                    table.tableName,
                    "READ"
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={updateRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleTablePermission(
                    `TABLE_${
                      table.tableName
                    }_UPDATE_${updateRolePermission.permissionLevel.toUpperCase()}`,
                    cellLocation,
                    updateRolePermission.RolePermissionId,
                    table.tableName,
                    "UPDATE"
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={deleteRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleTablePermission(
                    `TABLE_${
                      table.tableName
                    }_DELETE_${deleteRolePermission.permissionLevel.toUpperCase()}`,
                    cellLocation,
                    deleteRolePermission.RolePermissionId,
                    table.tableName,
                    "DELETE"
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={assignRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleTablePermission(
                    `TABLE_${
                      table.tableName
                    }_ASSIGN_${assignRolePermission.permissionLevel.toUpperCase()}`,
                    cellLocation,
                    assignRolePermission.RolePermissionId,
                    table.tableName,
                    "ASSIGN"
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={importRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleBinaryPermission(
                    cellLocation,
                    `TABLE_${table.tableName}_IMPORT`,
                    importRolePermission.RolePermissionId ?? "",
                    importRolePermission.permissionLevel === "none"
                      ? false
                      : true
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={exportRolePermission.permissionLevel}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleBinaryPermission(
                    cellLocation,
                    `TABLE_${table.tableName}_EXPORT`,
                    exportRolePermission.RolePermissionId ?? "",
                    exportRolePermission.permissionLevel === "none"
                      ? false
                      : true
                  )
                }
              ></PermissionIcon>
            ),
            isReadOnly: true,
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
        ],
      } as Row;
      rows.push(row);
    }

    adminPermission.setRows(rows);
  };

  useEffect(() => {
    buildTable(props.state.rolePermissions, props.state.tables);
  }, [props.state.rolePermissions, props.state.tables]);

  return (
    <DataGrid
      rows={adminPermission.rows}
      columnWidths={[300, 100, 100, 100, 100, 100, 100, 100]}
      snapRows={1}
      snapColumns={1}
      editable={false}
      style={{
        borderBottomRightRadius: "0.2rem",
      }}
      styletr={{
        borderTopRightRadius: "0.2rem",
      }}
      styletl={{
        borderTopLeftRadius: "0.2rem",
      }}
      stylebl={{
        borderBottomLeftRadius: "0.2rem",
      }}
    ></DataGrid>
  );
};

export default EntityPermissions;
