import React, { useEffect, useState } from "react";
import {
  RolePermissionState,
  SystemAdministratorRoleId,
} from "../RolePermissions";
import { CellLocation, Row } from "../../components/datagrid/DataGridTypes";
import DataGrid from "../../components/DataGrid";
import PermissionIcon from "../PermissionIcon";
import useAdminPermission from "../../hooks/useAdminPermission";

interface JobPermissionsProps {
  roleId: string;
  state: RolePermissionState;
  setState: React.Dispatch<React.SetStateAction<RolePermissionState>>;
}

const JobPermissions = (props: JobPermissionsProps) => {
  const adminPermission = useAdminPermission(props);

  const buildTable = () => {
    const rows: Row[] = [];
    const row1: Row = {
      columns: [
        {
          value: "Permission Name",
          isReadOnly: true,
        },
        {
          value: "Access",
          isReadOnly: true,
          style: {
            justifyContent: "center",
          },
        },
      ],
    };
    rows.push(row1);

    const canUpdate = props.roleId !== SystemAdministratorRoleId;

    for (let permission of props.state.allPermissions) {
      if (permission.Tag !== "System") {
        continue;
      }
      if (!permission.Name.startsWith("JOB_")) {
        continue;
      }

      let rolePermissionId = "";
      let hasPermission = false;
      for (let rolePermission of props.state.rolePermissions) {
        if (rolePermission.PermissionId === permission.PermissionId) {
          rolePermissionId = rolePermission.RolePermissionId as string;
          hasPermission = true;
          break;
        }
      }

      const row: Row = {
        columns: [
          {
            value: permission.Name,
            isReadOnly: true,
            style: {
              border: "none",
            },
          },
          {
            custom: (value: string, cellLocation: CellLocation) => (
              <PermissionIcon
                icon={rolePermissionId ? `system` : `none`}
                onClick={() =>
                  canUpdate &&
                  adminPermission.toggleBinaryPermission(
                    cellLocation,
                    permission.Name,
                    rolePermissionId,
                    hasPermission
                  )
                }
              ></PermissionIcon>
            ),
            style: {
              justifyContent: "center",
              border: "none",
              userSelect: "none",
            },
          },
        ],
      };
      rows.push(row);
    }
    adminPermission.setRows(rows);
  };

  useEffect(() => {
    buildTable();
  }, []);
  return (
    <DataGrid
      rows={adminPermission.rows}
      columnWidths={[300, 150]}
      snapColumns={1}
      snapRows={1}
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
    />
  );
};

export default JobPermissions;
