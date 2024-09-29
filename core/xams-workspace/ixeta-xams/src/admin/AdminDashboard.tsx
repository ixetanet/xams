import { TablesResponse } from "../api/TablesResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import {
  AppShell,
  Button,
  Header,
  Loader,
  Navbar,
  ScrollArea,
} from "@mantine/core";
import React, { useEffect } from "react";
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

const AdminDashboard = (props: AdminDashboardProps) => {
  const authRequest = useAuthRequest();
  const [hasAccess, setHasAccess] = React.useState<boolean | undefined>(
    undefined
  );
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
    if (!(await authRequest.hasAllPermissions(["ACCESS_ADMIN_DASHBOARD"]))) {
      setHasAccess(false);
      return;
    }

    setHasAccess(true);

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

  if (hasAccess === undefined) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  if (!hasAccess) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        You don&apos;t have permission to view this page. Please contact your
        system administrator.
      </div>
    );
  }

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
      order: 500,
      navLink: <AdminDashJobs />,
    },
    {
      order: 600,
      navLink: <AdminDashSettings />,
    },
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
        styles={(theme) => ({
          main: {
            backgroundColor:
              theme.colorScheme === "dark"
                ? theme.colors.dark[8]
                : theme.colors.gray[0],
            overflow: "hidden",
            display: "flex",
          },
        })}
        header={
          <Header height={60} p="xs">
            <div className="w-full h-full flex items-center justify-between text-xl">
              <div className=" pl-2">{props.title ?? `Admin Dashboard`}</div>
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
          </Header>
        }
        navbar={
          <Navbar width={{ base: 320 }} p="xs">
            <Navbar.Section grow component={ScrollArea} mx="-xs" px="xs">
              {sortedNavLinks.map((navItem, i) => {
                return <span key={i}>{navItem.navLink}</span>;
              })}
            </Navbar.Section>
          </Navbar>
        }
      >
        <div className="flex w-full">
          {activeComponent != null && activeComponent.component}
        </div>
      </AppShell>
    </AdminDashContext.Provider>
  );
};

export default AdminDashboard;
