import React, { useCallback, useEffect, useState } from "react";
import useAuthRequest from "../hooks/useAuthRequest";
import PermissionIcon from "./PermissionIcon";
import Query from "../utils/Query";
import { RolePermission } from "../api/RolePermission";
import { Loader, Tabs } from "@mantine/core";
import { Permission } from "../api/Permission";
import { SaveEventResponse, useFormBuilderType } from "../hooks/useFormBuilder";
import Field from "../components/Field";
import SaveButton from "../components/SaveButton";
import EntityPermissions from "./components/EntityPermissions";
import CustomPermissions from "./components/CustomPermissions";
import JobPermissions from "./components/JobPermissions";
import SystemPermissions from "./components/SystemPermissions";
import ActionPermissions from "./components/ActionPermissions";
import HubPermissions from "./components/HubPermissions";
import CopyId from "./components/CopyId";
import { RolePermissionState } from "./types/RolePermissionTypes";
import { TablesResponse } from "../api/TablesResponse";

interface RolePermissionsProps {
  roleId?: string;
  formBuilder: useFormBuilderType;
}

const RolePermissions = (props: RolePermissionsProps) => {
  const authRequest = useAuthRequest();

  const [isLoading, setIsLoading] = React.useState<boolean>(true);
  const [state, setState] = useState<RolePermissionState>({
    tables: [] as TablesResponse[],
    allPermissions: [] as Permission[],
    rolePermissions: [] as RolePermission[],
    createRolePermissions: [] as RolePermission[],
    updateRolePermissions: [] as RolePermission[],
    deleteRolePermissions: [] as RolePermission[],
    isLoaded: false,
  });
  const [activeTab, setActiveTab] = useState<string | null>("entities");

  const onLoad = async () => {
    if (props.roleId == null) {
      setIsLoading(false);
      return;
    }
    const tablesResp = await authRequest.tables();
    if (!tablesResp.succeeded) {
      return;
    }

    const permissionsQuery = new Query(["*"])
      .top(999999)
      .from("Permission")
      .orderBy("Name", "asc")
      .distinct()
      .toReadRequest();
    const permissionsResp = await authRequest.read<Permission>(
      permissionsQuery
    );

    const query = new Query(["*"])
      .top(999999)
      .from("RolePermission")
      .where("RoleId", "==", props.roleId)
      .distinct()
      .toReadRequest();
    const rolePermissionsResp = await authRequest.read<RolePermission>(query);
    if (!rolePermissionsResp.succeeded) {
      return;
    }
    setState((prev) => {
      return {
        ...prev,
        tables: tablesResp.data as TablesResponse[],
        allPermissions: permissionsResp.data.results,
        rolePermissions: rolePermissionsResp.data.results,
        isLoaded: true,
      };
    });

    setIsLoading(false);
  };

  const onSave = useCallback(async (): Promise<SaveEventResponse> => {
    const creates = state.createRolePermissions.map((x) => {
      return {
        tableName: "RolePermission",
        fields: {
          PermissionId: x.PermissionId,
          RoleId: x.RoleId,
        },
      };
    });
    const updates = state.updateRolePermissions.map((x) => {
      return {
        tableName: "RolePermission",
        fields: x,
      };
    });
    const deletes = state.deleteRolePermissions.map((x) => {
      return {
        tableName: "RolePermission",
        fields: x,
      };
    });
    if (creates.length === 0 && updates.length === 0 && deletes.length === 0) {
      return {
        continue: true,
      };
    }
    setIsLoading(true);
    const resp = await authRequest.bulk({
      creates: creates,
      updates: updates,
      deletes: deletes,
    });
    if (!resp.succeeded) {
      setIsLoading(false);
      return {
        continue: false,
      };
    }
    setIsLoading(false);
    return {
      continue: true,
    };
  }, [
    state.deleteRolePermissions,
    state.createRolePermissions,
    state.updateRolePermissions,
  ]);

  useEffect(() => {
    onLoad();
  }, []);

  props.formBuilder.onPreSaveRef.current = onSave;

  return (
    <div className="w-full flex flex-col gap-4">
      <Field name="Name"></Field>
      <div className="w-full h-[500px] relative">
        {isLoading && (
          <div className="w-full h-full absolute flex justify-center items-center">
            <Loader />
          </div>
        )}
        {props.roleId == null && (
          <div className="w-full h-full flex justify-center items-center">
            Save the role to set permissions
          </div>
        )}

        {props.roleId != null && state.isLoaded && (
          <>
            <Tabs
              defaultValue="entities"
              className="h-full flex flex-col"
              onChange={setActiveTab}
            >
              <Tabs.List>
                <Tabs.Tab value="entities">Entities</Tabs.Tab>
                <Tabs.Tab value="actions">Actions</Tabs.Tab>
                <Tabs.Tab value="jobs">Jobs</Tabs.Tab>
                <Tabs.Tab value="hubs">Hubs</Tabs.Tab>
                <Tabs.Tab value="system">System</Tabs.Tab>
                <Tabs.Tab value="permissions">Custom Permissions</Tabs.Tab>
              </Tabs.List>

              <Tabs.Panel value="entities" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <EntityPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></EntityPermissions>
                </div>
              </Tabs.Panel>

              <Tabs.Panel value="actions" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <ActionPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></ActionPermissions>
                </div>
              </Tabs.Panel>

              <Tabs.Panel value="permissions" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <CustomPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></CustomPermissions>
                </div>
              </Tabs.Panel>

              <Tabs.Panel value="jobs" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <JobPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></JobPermissions>
                </div>
              </Tabs.Panel>

              <Tabs.Panel value="hubs" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <HubPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></HubPermissions>
                </div>
              </Tabs.Panel>

              <Tabs.Panel value="system" pt="xs" className="h-full">
                <div className="w-full h-full">
                  <SystemPermissions
                    roleId={props.roleId}
                    state={state}
                    setState={setState}
                  ></SystemPermissions>
                </div>
              </Tabs.Panel>
            </Tabs>
          </>
        )}
      </div>

      <div className="flex justify-between">
        <div className="flex gap-5 w-full">
          {props.roleId != null &&
            activeTab != null &&
            activeTab === "entities" && (
              <>
                <span className="flex text-sm gap-1 items-center">
                  <PermissionIcon icon="none"></PermissionIcon>
                  None
                </span>
                <span className="flex text-sm gap-1 items-center">
                  <PermissionIcon icon="user"></PermissionIcon>
                  User
                </span>
                <span className="flex text-sm gap-1 items-center">
                  <PermissionIcon icon="team"></PermissionIcon>
                  Team
                </span>
                <span className="flex text-sm gap-1 items-center">
                  <PermissionIcon icon="system"></PermissionIcon>
                  System
                </span>
              </>
            )}
          {props.roleId != null &&
            activeTab != null &&
            (activeTab === "permissions" || activeTab === "system" || activeTab === "hubs") && (
              <>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="none"></PermissionIcon>
                  Doesn't have permission
                </span>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="system"></PermissionIcon>
                  Has permission
                </span>
              </>
            )}
          {props.roleId != null &&
            activeTab != null &&
            activeTab === "actions" && (
              <>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="none"></PermissionIcon>
                  Can't execute action
                </span>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="system"></PermissionIcon>
                  Can execute action
                </span>
              </>
            )}
          {props.roleId != null &&
            activeTab != null &&
            activeTab === "jobs" && (
              <>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="none"></PermissionIcon>
                  Can't start job
                </span>
                <span className="flex text-sm gap-1 items-center whitespace-nowrap">
                  <PermissionIcon icon="system"></PermissionIcon>
                  Can start job
                </span>
              </>
            )}
          {props.formBuilder.operation === "UPDATE" && (
            <div className="w-full flex justify-start items-center pr-2">
              <CopyId
                value={
                  props.formBuilder.data[`${props.formBuilder.tableName}Id`]
                }
              />
            </div>
          )}
        </div>
        <div>
          <SaveButton></SaveButton>
        </div>
      </div>
    </div>
  );
};

export default RolePermissions;
