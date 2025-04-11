import { Button, Menu } from "@mantine/core";
import {
  IconTableExport,
  IconFileImport,
  IconDotsVertical,
  IconFileExport,
  IconColumns,
} from "@tabler/icons-react";
import React, { useEffect, useState } from "react";
import useColor from "../../hooks/useColor";
import usePermissionStore from "../../stores/usePermissionStore";
import { useDataTableContext } from "../DataTableImp";
import useAuthRequest from "../../hooks/useAuthRequest";
import DataTableImportData from "./DataTableImportData";
import { useDisclosure } from "@mantine/hooks";
import { ReadRequest } from "../../api/ReadRequest";
import DataTableColumns from "./DataTableColumns";

interface ShowOptions {
  Import: boolean;
  Export: boolean;
}

const DataTableOptions = () => {
  const [visibleOptions, setVisibleOptions] = useState<ShowOptions | undefined>(
    undefined
  );
  const color = useColor().getIconColor();
  const authRequest = useAuthRequest();
  const ctx = useDataTableContext();
  const permissionStore = usePermissionStore();
  const [importDataOpened, importDataDisclosure] = useDisclosure(false);
  const [columnsOpened, columnsDisclosure] = useDisclosure(false);

  const getPermissions = async () => {
    const permissions = await permissionStore.getPermissions(authRequest, [
      `TABLE_${ctx.props.tableName}_IMPORT`,
      `TABLE_${ctx.props.tableName}_EXPORT`,
      `ACTION_TABLE_ImportData`,
      `ACTION_TABLE_ExportData`,
    ]);
    const tablePermissions = await permissionStore.getTablePermissions(
      authRequest,
      ctx.props.tableName
    );

    let canImport = false;
    let canExport = false;

    if (
      permissions.find((p) => p === `TABLE_${ctx.props.tableName}_IMPORT`) &&
      permissions.find((p) => p === `ACTION_TABLE_ImportData`) &&
      tablePermissions.create !== "NONE" &&
      tablePermissions.update !== "NONE" &&
      (ctx.props.canImport == null || ctx.props.canImport)
    ) {
      canImport = true;
    }

    if (
      permissions.find((p) => p === `TABLE_${ctx.props.tableName}_EXPORT`) &&
      permissions.find((p) => p === `ACTION_TABLE_ExportData`) &&
      tablePermissions.read !== "NONE" &&
      (ctx.props.canExport == null || ctx.props.canExport)
    ) {
      canExport = true;
    }

    setVisibleOptions({
      Import: canImport,
      Export: canExport,
    });
  };

  const downloadImportTemplate = async () => {
    await authRequest.action(
      `TABLE_ImportTemplate`,
      {
        tableName: ctx.props.tableName,
      },
      `ImportTemplate_${ctx.props.tableName}.xlsx`
    );
  };

  const downloadExport = async () => {
    await authRequest.action(
      `TABLE_ExportData`,
      {
        query: {
          tableName: ctx.props.tableName,
          fields: ["*"],
          orderBy: ctx.props.orderBy,
          filters: ctx.props.filters,
          joins: ctx.props.joins,
          except: ctx.props.except,
          maxResults: 999999,
        } as ReadRequest,
      },
      `ExportData_${ctx.props.tableName}.xlsx`
    );
  };

  useEffect(() => {
    if (!visibleOptions) {
      getPermissions();
    }
  }, [visibleOptions, ctx.state.isLoadingData]);

  if (ctx.props.disabledMessage || ctx.state.metadata == null) {
    return <></>;
  }

  return (
    <>
      <DataTableImportData
        opened={importDataOpened}
        close={importDataDisclosure.close}
      />
      {ctx.state.metadata != null && (
        <DataTableColumns
          opened={columnsOpened}
          close={columnsDisclosure.close}
        />
      )}
      <Menu shadow="md" width={200}>
        <Menu.Target>
          <Button
            radius="xl"
            variant="subtle"
            styles={{
              root: {
                padding: 6,
              },
            }}
          >
            <IconDotsVertical size={24} strokeWidth={2} color={color} />
          </Button>
        </Menu.Target>

        <Menu.Dropdown>
          <Menu.Label>View</Menu.Label>
          <Menu.Item
            onClick={columnsDisclosure.open}
            leftSection={<IconColumns size={14} />}
          >
            Columns
          </Menu.Item>
          {(visibleOptions?.Import || visibleOptions?.Export) && (
            <Menu.Label>Data</Menu.Label>
          )}
          {visibleOptions?.Import && (
            <>
              <Menu.Item
                leftSection={<IconTableExport size={14} />}
                onClick={downloadImportTemplate}
              >
                Import Template
              </Menu.Item>
              <Menu.Item
                leftSection={<IconFileImport size={14} />}
                onClick={importDataDisclosure.open}
              >
                Import Data
              </Menu.Item>
            </>
          )}
          {visibleOptions?.Export && (
            <>
              <Menu.Item
                leftSection={<IconFileExport size={14} />}
                onClick={downloadExport}
              >
                Export Data
              </Menu.Item>
            </>
          )}
        </Menu.Dropdown>
      </Menu>
    </>
  );
};

export default DataTableOptions;
