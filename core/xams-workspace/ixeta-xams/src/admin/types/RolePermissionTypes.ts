import { Permission } from "../../api/Permission";
import { RolePermission } from "../../api/RolePermission";
import { TablesResponse } from "../../api/TablesResponse";

export type RolePermissionState = {
  tables: TablesResponse[];
  allPermissions: Permission[];
  rolePermissions: RolePermission[];
  createRolePermissions: RolePermission[];
  updateRolePermissions: RolePermission[];
  deleteRolePermissions: RolePermission[];
  isLoaded: boolean;
};

export const SystemAdministratorRoleId = "64589861-0481-4dbb-a96f-9b8b6546c40d";
