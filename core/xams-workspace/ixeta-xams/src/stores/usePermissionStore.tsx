import { API_DATA_READ, API_DATA_PERMISSIONS } from "../apiurls";
import { useAuthRequestType } from "../hooks/useAuthRequest";
import { create } from "zustand";
import { PermissionUtil } from "../utils/PermissionUtil";
import Query from "../utils/Query";

export const readPermisions = `TABLE_{tableName}_READ_SYSTEM, TABLE_{tableName}_READ_TEAM, TABLE_{tableName}_READ_USER`;
export const createPermissions = `TABLE_{tableName}_CREATE_SYSTEM, TABLE_{tableName}_CREATE_TEAM, TABLE_{tableName}_CREATE_USER`;
export const deletePermissions = `TABLE_{tableName}_DELETE_SYSTEM, TABLE_{tableName}_DELETE_TEAM, TABLE_{tableName}_DELETE_USER`;
export const updatePermissions = `TABLE_{tableName}_UPDATE_SYSTEM, TABLE_{tableName}_UPDATE_TEAM, TABLE_{tableName}_UPDATE_USER`;

export interface TablePermissions {
  read: PermissionLevel;
  create: PermissionLevel;
  delete: PermissionLevel;
  update: PermissionLevel;
}

export type PermissionLevel = "SYSTEM" | "TEAM" | "USER" | "NONE";

export interface TablePermission {
  tableName: string;
  permissions: TablePermissions;
}

export interface usePermissionStoreState {
  tablePermissions: TablePermission[];
  teams: string[];
  getTablePermissions: (
    authRequest: useAuthRequestType,
    tableName: string
  ) => Promise<TablePermissions>;
  getPermissions: (
    authRequest: useAuthRequestType,
    permissions: string[]
  ) => Promise<string[]>;
  getTeams: (authRequest: useAuthRequestType) => Promise<string[]>;
}

export const usePermissionStore = create<usePermissionStoreState>()(
  (set, get) => ({
    tablePermissions: [],
    teams: [],
    getTablePermissions: async (
      authRequest: useAuthRequestType,
      tableName: string
    ) => {
      const tablePermission = get().tablePermissions.find(
        (p) => p.tableName === tableName
      );
      if (tablePermission !== undefined) {
        return tablePermission.permissions;
      }

      const permissions = `${readPermisions.replaceAll(
        "{tableName}",
        tableName
      )}, ${createPermissions.replaceAll(
        "{tableName}",
        tableName
      )}, ${deletePermissions.replaceAll(
        "{tableName}",
        tableName
      )}, ${updatePermissions.replaceAll("{tableName}", tableName)}`;

      const resp = await authRequest.execute({
        url: API_DATA_PERMISSIONS,
        method: "POST",
        body: {
          method: "has_permissions",
          parameters: {
            permissionNames: permissions.split(",").map((p) => p.trim()),
          },
        },
      });
      if (resp?.succeeded === true) {
        const permissionsData = resp.data as string[];
        if (permissionsData.length > 0) {
          const readPms = permissionsData.filter((p) =>
            p.startsWith(`TABLE_${tableName}_READ`)
          );
          const createPms = permissionsData.filter((p) =>
            p.startsWith(`TABLE_${tableName}_CREATE`)
          );
          const deletePms = permissionsData.filter((p) =>
            p.startsWith(`TABLE_${tableName}_DELETE`)
          );
          const updatePms = permissionsData.filter((p) =>
            p.startsWith(`TABLE_${tableName}_UPDATE`)
          );

          const results = {
            read: PermissionUtil.getHighestPermissionLevel(readPms),
            create: PermissionUtil.getHighestPermissionLevel(createPms),
            delete: PermissionUtil.getHighestPermissionLevel(deletePms),
            update: PermissionUtil.getHighestPermissionLevel(updatePms),
          } as TablePermissions;
          return results;
        }
      }
      const results = {
        read: "NONE",
        create: "NONE",
        delete: "NONE",
        update: "NONE",
      } as TablePermissions;
      return results;
    },
    getPermissions: async (
      authRequest: useAuthRequestType,
      permissions: string[]
    ) => {
      // Check permissions first
      let userPermissions = get().tablePermissions;
      let results = permissions.filter(
        (p) => userPermissions.find((up) => up.tableName === p) != null
      );
      if (results.length === permissions.length) {
        return results;
      }

      const resp = await authRequest.execute({
        url: API_DATA_PERMISSIONS,
        method: "POST",
        body: {
          method: "has_permissions",
          parameters: {
            permissionNames: permissions,
          },
        },
      });
      if (resp?.succeeded === true) {
        const permissionsData = resp.data as string[];
        return permissionsData;
      }
      return [];
    },
    getTeams: async (authRequest: useAuthRequestType) => {
      if (get().teams.length > 0) {
        return get().teams;
      }

      const query = new Query(["Name"])
        .top(99999)
        .from("Team")
        .join("Team.TeamId", "TeamUser.TeamId", "tu")
        .join("tu.UserId", "User.UserId", "u")
        .distinct()
        .toReadRequest();

      const resp = await authRequest.read<any>(query);

      if (!resp.succeeded) {
        return [];
      }

      set({ teams: resp.data.results.map((d: any) => d.Name) ?? [] });

      return resp.data.results.map((d: any) => d.Name) ?? [];
    },
  })
);

export type usePermissionStoreType = ReturnType<typeof usePermissionStore>;
export default usePermissionStore;
