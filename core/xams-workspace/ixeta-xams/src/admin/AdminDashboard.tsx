import { TablesResponse } from "../api/TablesResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import { AppShell, Burger, Button, Loader, ScrollArea } from "@mantine/core";
import React, { ReactNode, useEffect } from "react";
import { useDisclosure } from "@mantine/hooks";
import ExportDataModal from "./ExportDataModal";
import ImportDataModal from "./ImportDataModal";
import ToggleMode from "../components/ToggleMode";
import { DataTableProps } from "../components/datatable/DataTableTypes";
import useColor from "../hooks/useColor";
import AdminDashEntities from "./nav/AdminDashEntities";
import AdminDashSecurity from "./nav/AdminDashSecurity";
import AdminDashOptions from "./nav/AdminDashOptions";
import AdminDashJobs from "./nav/AdminDashJobs";
import AdminDashSettings from "./nav/AdminDashSettings";
import AdminDashAudit from "./nav/AdminDashAudit";
import AdminDashServers from "./nav/AdminDashServers";
import AdminDashDevelopment from "./nav/AdminDashDevelopment";
import AdminDashLogs from "./nav/AdminDashLogs";
import LogsViewer from "./components/logviewer/LogsViewer";

const EmptyTableInfo = {
  tableName: "",
  appendCustomForm: undefined,
  formAppendButton: undefined,
  formMaxWidth: undefined,
  formCloseOnCreate: undefined,
  formTitle: undefined,
  fields: undefined,
  filters: undefined,
  formFields: undefined,
  formLookupQueries: undefined,
  orderBy: undefined,
  refreshInterval: undefined,
  canCreate: undefined,
  canDelete: undefined,
  maxResults: 50,
} as DataTableProps;

export interface AdminDashboardProps {
  title?: string;
  visibleEntities?: string[];
  showEntityDisplayNames?: boolean;
  addMenuItems?: NavItem[];
  hiddenEntities?: string[];
  hiddenMenuItems?: string[];
  forceHideImportData?: boolean;
  forceHideExportData?: boolean;
  forceHideToggleMode?: boolean;
  accessDeniedMessage?: ReactNode;
}

export interface NavItem {
  order: number;
  navLink: React.JSX.Element;
}

export interface ContextProps {
  children?: any;
}

export type AdminDashContextShape = {
  props: AdminDashboardProps;
  tables: TablesResponse[];
  color: string;
  setActiveComponent: React.Dispatch<
    React.SetStateAction<
      | {
          component: React.ReactNode;
        }
      | undefined
    >
  >;
  emptyTableInfo: any;
};

export const AdminDashContext =
  React.createContext<AdminDashContextShape | null>(null);

export const useAdminDashContext = () => {
  const context = React.useContext(AdminDashContext);
  if (!context) {
    throw new Error(
      "useAdminDashContext must be used within a AdminDashContextProvider"
    );
  }
  return context;
};

type Access = {
  Dashboard: boolean;
  Development: boolean;
};

const AdminDashboard = (props: AdminDashboardProps) => {
  const authRequest = useAuthRequest();
  const [opened, { toggle }] = useDisclosure();
  const [access, setAccess] = React.useState<Access | undefined>(undefined);
  const [activeComponent, setActiveComponent] = React.useState<
    | {
        component: React.ReactNode;
      }
    | undefined
  >(undefined);
  const [tables, setTables] = React.useState<TablesResponse[]>([]);
  const [exportDataOpened, exportData] = useDisclosure(false);
  const [importDataOpened, importData] = useDisclosure(false);
  const [canExportImport, setCanExportImport] = React.useState<{
    canImport: boolean;
    canExport: boolean;
  }>({
    canImport: false,
    canExport: false,
  });
  const color = useColor().getIconColor();

  const getTables = async () => {
    const resp = await authRequest.tables();
    if (resp.succeeded === true) {
      setTables(resp.data as TablesResponse[]);
    }
  };

  const getPermissions = async () => {
    const dashReq = authRequest.hasAllPermissions(["ACCESS_ADMIN_DASHBOARD"]);
    const devReq = authRequest.hasAllPermissions(["ACCESS_ADMIN_DEVELOPMENT"]);
    const [dashResp, devResp] = await Promise.all([dashReq, devReq]);

    if (!dashResp) {
      setAccess({
        Dashboard: false,
        Development: false,
      });
      return;
    }

    setAccess({
      Dashboard: dashResp,
      Development: devResp,
    });

    let canExport = false;
    let canImport = false;
    if (
      (await authRequest.hasAllPermissions([
        "ACTION_ADMIN_ExportDependencies",
        "ACTION_ADMIN_ExportData",
      ])) === true
    ) {
      canExport = true;
    }
    if (await authRequest.hasAllPermissions(["ACTION_ADMIN_ImportData"])) {
      canImport = true;
    }
    setCanExportImport({
      canImport: canImport,
      canExport: canExport,
    });
  };

  useEffect(() => {
    getPermissions();
    getTables();
  }, []);

  if (access?.Dashboard === undefined) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  if (!access.Dashboard) {
    if (props.accessDeniedMessage) {
      return <>{props.accessDeniedMessage}</>;
    } else {
      return (
        <div className="w-full h-full flex justify-center items-center">
          You don&apos;t have permission to view this page. Please contact your
          system administrator.
        </div>
      );
    }
  }

  const logsEnabled = tables.find((t) => t.tableName === "Log") != null;

  let navLinks = [
    {
      order: 100,
      navLink: <AdminDashEntities />,
    },
    {
      order: 200,
      navLink: <AdminDashSecurity />,
    },
    {
      order: 300,
      navLink: <AdminDashOptions />,
    },
    {
      order: 400,
      navLink: <AdminDashAudit />,
    },
    {
      order: 600,
      navLink: <AdminDashJobs />,
    },
    {
      order: 700,
      navLink: <AdminDashSettings />,
    },
    {
      order: 800,
      navLink: <AdminDashServers />,
    },
    ...(logsEnabled
      ? [
          {
            order: 900,
            navLink: <AdminDashLogs />,
          },
        ]
      : []),

    ...(access.Development === true
      ? [
          {
            order: 1000,
            navLink: <AdminDashDevelopment />,
          },
        ]
      : []),
  ] as NavItem[];

  if (props.addMenuItems !== undefined) {
    navLinks = navLinks.concat(props.addMenuItems);
  }
  const sortedNavLinks = navLinks.sort((a, b) => a.order - b.order);

  return (
    <AdminDashContext.Provider
      value={{
        props: props,
        tables: tables,
        color: color,
        setActiveComponent: setActiveComponent,
        emptyTableInfo: EmptyTableInfo,
      }}
    >
      <ExportDataModal opened={exportDataOpened} close={exportData.close} />
      <ImportDataModal opened={importDataOpened} close={importData.close} />

      <AppShell
        padding="md"
        header={{ height: 60 }}
        navbar={{
          width: 300,
          breakpoint: "sm",
          collapsed: { mobile: !opened },
        }}
        styles={{
          navbar: {
            overflowY: "auto",
          },
        }}
      >
        <AppShell.Header>
          <div className="w-full h-full flex items-center justify-between text-xl px-2">
            <div className="flex pl-2">
              <Burger
                opened={opened}
                onClick={toggle}
                hiddenFrom="sm"
                size="sm"
              />
              {props.title ?? `Admin Dashboard`}
            </div>
            <div className="flex items-center gap-2">
              {!props.forceHideToggleMode && <ToggleMode></ToggleMode>}
              {canExportImport.canImport === true &&
                !props.forceHideImportData && (
                  <Button variant="outline" onClick={importData.open}>
                    Import Data
                  </Button>
                )}
              {canExportImport.canExport === true &&
                !props.forceHideExportData && (
                  <Button onClick={exportData.open}>Export Data</Button>
                )}
            </div>
          </div>
        </AppShell.Header>
        <AppShell.Navbar className=" p-2">
          {sortedNavLinks.map((navItem, i) => {
            return <span key={i}>{navItem.navLink}</span>;
          })}
        </AppShell.Navbar>
        <AppShell.Main
          styles={{
            main: {
              width: "100%",
              height: "100%",
              display: "flex",
              flexDirection: "column",
            },
          }}
        >
          <div className="w-full h-1 grow">
            {activeComponent != null ? (
              activeComponent.component
            ) : (
              <>{logsEnabled && <LogsViewer />}</>
            )}
          </div>
        </AppShell.Main>
      </AppShell>
    </AdminDashContext.Provider>
  );
};

export default AdminDashboard;
