import { PermissionLevel } from "../stores/usePermissionStore";

export const PermissionUtil = {
  getHighestPermissionLevel: (permissions: string[]): PermissionLevel => {
    if (permissions.find((p) => p.endsWith("_SYSTEM")) !== undefined) {
      return "SYSTEM";
    }
    if (permissions.find((p) => p.endsWith("_TEAM")) !== undefined) {
      return "TEAM";
    }
    if (permissions.find((p) => p.endsWith("_USER")) !== undefined) {
      return "USER";
    }
    return "NONE";
  },
};
