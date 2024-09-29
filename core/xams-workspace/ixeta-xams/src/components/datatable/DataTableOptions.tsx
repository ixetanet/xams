import { Button, Menu, Text } from "@mantine/core";
import {
  IconTableExport,
  IconFileImport,
  IconDotsVertical,
  IconMessageCircle,
  IconFileExport,
} from "@tabler/icons-react";
import React, { useEffect, useState } from "react";
import useColor from "../../hooks/useColor";
import usePermissionStore from "../../stores/usePermissionStore";
import { useDataTableContext } from "../DataTableImp";
import useAuthRequest from "../../hooks/useAuthRequest";
import { API_DATA_ACTION } from "../../apiurls";
import DataTableImportData from "./DataTableImportData";
import { useDisclosure } from "@mantine/hooks";
import { ReadRequest } from "../../api/ReadRequest";

interface ShowOptions {
  Import: boolean;
  Export: boolean;
}

const DataTableOptions = () => {
  const [show, setShow] = useState<boolean>(false);
  const [visibleOptions, setVisibleOptions] = useState<ShowOptions | undefined>(
    undefined
  );
  const color = useColor().getIconColor();
  const authRequest = useAuthRequest();
  const ctx = useDataTableContext();
  const permissionStore = usePermissionStore();
  const [importDataOpened, importDataDisclosure] = useDisclosure(false);

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
      tablePermissions.read !== "NONE"
    ) {
      canExport = true;
    }

    setVisibleOptions({
      Import: canImport,
      Export: canExport,
    });

    if (ctx.props.disabledMessage == null && (canImport || canExport)) {
      setShow(true);
    }
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

  useEffect(() => {
    if (ctx.props.disabledMessage == null) {
      if (visibleOptions?.Import || visibleOptions?.Export) {
        setShow(true);
      }
    }
  }, [ctx.props.disabledMessage]);

  if (!show || ctx.state.metadata == null) {
    return <></>;
  }

  return (
    <>
      <DataTableImportData
        opened={importDataOpened}
        close={importDataDisclosure.close}
      />
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
          {(visibleOptions?.Import || visibleOptions?.Export) && (
            <Menu.Label>Data</Menu.Label>
          )}
          {visibleOptions?.Import && (
            <>
              <Menu.Item
                icon={<IconTableExport size={14} />}
                onClick={downloadImportTemplate}
              >
                Import Template
              </Menu.Item>
              <Menu.Item
                icon={<IconFileImport size={14} />}
                onClick={importDataDisclosure.open}
              >
                Import Data
              </Menu.Item>
            </>
          )}
          {visibleOptions?.Export && (
            <>
              <Menu.Item
                icon={<IconFileExport size={14} />}
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