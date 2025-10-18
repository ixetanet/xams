import React, { ReactNode } from "react";
import { TablesResponse } from "../../api/TablesResponse";

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
  userCard?: ReactNode;
  accessDeniedMessage?: ReactNode;
}

export interface NavItem {
  order: number;
  navLink: React.JSX.Element;
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
