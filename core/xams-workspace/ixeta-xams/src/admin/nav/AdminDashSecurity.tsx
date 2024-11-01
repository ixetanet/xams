import { NavLink } from "@mantine/core";
import {
  IconLock,
  IconUser,
  IconUsers,
  IconKey,
  IconUserCircle,
} from "@tabler/icons-react";
import React from "react";
import RolePermissions from "../RolePermissions";
import { useAdminDashContext } from "../AdminDashboard";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";

const AdminDashSecurity = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) =>
        ["User", "Role", "Team", "Permission"].includes(table.tableName)
      ).length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Security") && (
          <NavLink
            label="Security"
            icon={<IconLock size={16} strokeWidth={2} color={ctx.color} />}
          >
            {ctx.tables.find((table) => table.tableName === "User") &&
              !ctx.props.hiddenMenuItems?.includes("Users") && (
                <NavLink
                  label="Users"
                  onClick={() =>
                    ctx.setActiveComponent({
                      component: (
                        <div className="w-full h-full flex flex-col">
                          <div className="grow h-1">
                            <DataTable
                              tableName="User"
                              maxResults={100}
                              formMaxWidth={72}
                              formAppendButton={(formbuilder) => {
                                return (
                                  <>
                                    {formbuilder.operation === "UPDATE" && (
                                      <div className="w-full flex justify-start items-center gap-1">
                                        <CopyId
                                          value={
                                            formbuilder.data[
                                              `${formbuilder.tableName}Id`
                                            ]
                                          }
                                        />
                                      </div>
                                    )}
                                  </>
                                );
                              }}
                              appendCustomForm={(formbuilder) => {
                                if (formbuilder.snapshot === undefined) {
                                  return <></>;
                                }
                                return (
                                  <div className=" h-96 mt-5 flex justify-stretch gap-4">
                                    <DataTable
                                      title="Roles"
                                      tableName={"UserRole"}
                                      fields={["RoleId"]}
                                      filters={[
                                        {
                                          field: "UserId",
                                          value: formbuilder.snapshot.UserId,
                                        },
                                      ]}
                                      formZIndex={1000}
                                      formFields={["RoleId"]}
                                      formFieldDefaults={[
                                        {
                                          field: "UserId",
                                          value: formbuilder.snapshot.UserId,
                                        },
                                      ]}
                                      formLookupQueries={[
                                        {
                                          field: "RoleId",
                                          except: [
                                            {
                                              fromField: "RoleId",
                                              query: {
                                                tableName: "UserRole",
                                                fields: ["RoleId"],
                                                filters: [
                                                  {
                                                    field: "UserId",
                                                    operator: "==",
                                                    value:
                                                      formbuilder.snapshot
                                                        .UserId,
                                                  },
                                                ],
                                              },
                                            },
                                          ],
                                        },
                                      ]}
                                    ></DataTable>
                                    <DataTable
                                      title="Teams"
                                      tableName={"TeamUser"}
                                      fields={["TeamId"]}
                                      filters={[
                                        {
                                          field: "UserId",
                                          value: formbuilder.snapshot.UserId,
                                        },
                                      ]}
                                      formZIndex={1000}
                                      formFields={["TeamId"]}
                                      formFieldDefaults={[
                                        {
                                          field: "UserId",
                                          value: formbuilder.snapshot.UserId,
                                        },
                                      ]}
                                      formLookupQueries={[
                                        {
                                          field: "TeamId",
                                          except: [
                                            {
                                              fromField: "TeamId",
                                              query: {
                                                tableName: "TeamUser",
                                                fields: ["TeamId"],
                                                filters: [
                                                  {
                                                    field: "UserId",
                                                    operator: "==",
                                                    value:
                                                      formbuilder.snapshot
                                                        .UserId,
                                                  },
                                                ],
                                              },
                                            },
                                          ],
                                        },
                                      ]}
                                    ></DataTable>
                                  </div>
                                );
                              }}
                            />
                          </div>
                        </div>
                      ),
                    })
                  }
                  icon={
                    <IconUser size={16} strokeWidth={2} color={ctx.color} />
                  }
                ></NavLink>
              )}

            {ctx.tables.find((table) => table.tableName === "Team") &&
              !ctx.props.hiddenMenuItems?.includes("Teams") && (
                <NavLink
                  label="Teams"
                  onClick={() =>
                    ctx.setActiveComponent({
                      component: (
                        <div className="w-full h-full flex flex-col">
                          <div className="grow h-1">
                            <DataTable
                              tableName="Team"
                              formCloseOnCreate={false}
                              maxResults={100}
                              formMaxWidth={72}
                              formAppendButton={(formbuilder) => {
                                return (
                                  <>
                                    {formbuilder.operation === "UPDATE" && (
                                      <div className="w-full flex justify-start items-center gap-1">
                                        <CopyId
                                          value={
                                            formbuilder.data[
                                              `${formbuilder.tableName}Id`
                                            ]
                                          }
                                        />
                                      </div>
                                    )}
                                  </>
                                );
                              }}
                              appendCustomForm={(formbuilder) => {
                                if (formbuilder.snapshot === undefined) {
                                  return <></>;
                                }
                                return (
                                  <div className=" h-96 mt-5 flex justify-stretch gap-4">
                                    <DataTable
                                      title="Roles"
                                      tableName={"TeamRole"}
                                      fields={["RoleId"]}
                                      filters={[
                                        {
                                          field: "TeamId",
                                          value: formbuilder.snapshot.TeamId,
                                        },
                                      ]}
                                      formZIndex={1000}
                                      formFields={["RoleId"]}
                                      formFieldDefaults={[
                                        {
                                          field: "TeamId",
                                          value: formbuilder.snapshot.TeamId,
                                        },
                                      ]}
                                      formLookupQueries={[
                                        {
                                          field: "RoleId",
                                          except: [
                                            {
                                              fromField: "RoleId",
                                              query: {
                                                tableName: "TeamRole",
                                                fields: ["RoleId"],
                                                filters: [
                                                  {
                                                    field: "TeamId",
                                                    operator: "==",
                                                    value:
                                                      formbuilder.snapshot
                                                        .TeamId,
                                                  },
                                                ],
                                              },
                                            },
                                          ],
                                        },
                                      ]}
                                    ></DataTable>
                                    <DataTable
                                      title="Users"
                                      tableName={"TeamUser"}
                                      fields={["UserId"]}
                                      filters={[
                                        {
                                          field: "TeamId",
                                          value: formbuilder.snapshot.TeamId,
                                        },
                                      ]}
                                      formZIndex={1000}
                                      formFields={["UserId"]}
                                      formFieldDefaults={[
                                        {
                                          field: "TeamId",
                                          value: formbuilder.snapshot.TeamId,
                                        },
                                      ]}
                                      formLookupQueries={[
                                        {
                                          field: "UserId",
                                          except: [
                                            {
                                              fromField: "UserId",
                                              query: {
                                                tableName: "TeamUser",
                                                fields: ["UserId"],
                                                filters: [
                                                  {
                                                    field: "TeamId",
                                                    operator: "==",
                                                    value:
                                                      formbuilder.snapshot
                                                        .TeamId,
                                                  },
                                                ],
                                              },
                                            },
                                          ],
                                        },
                                      ]}
                                    ></DataTable>
                                  </div>
                                );
                              }}
                            />
                          </div>
                        </div>
                      ),
                    })
                  }
                  icon={
                    <IconUsers size={16} strokeWidth={2} color={ctx.color} />
                  }
                ></NavLink>
              )}
            {ctx.tables.find((table) => table.tableName === "Role") &&
              !ctx.props.hiddenMenuItems?.includes("Roles") && (
                <NavLink
                  label="Roles"
                  onClick={() => {
                    ctx.setActiveComponent({
                      component: (
                        <DataTable
                          tableName="Role"
                          formMaxWidth={72}
                          maxResults={100}
                          // formCloseOnUpdate={false}
                          formCloseOnCreate={false}
                          customForm={(formbuilder) => {
                            return (
                              <RolePermissions
                                key={formbuilder.snapshot?.RoleId}
                                roleId={formbuilder.snapshot?.RoleId}
                                formBuilder={formbuilder}
                              ></RolePermissions>
                            );
                          }}
                        ></DataTable>
                      ),
                    });
                  }}
                  icon={
                    <IconUserCircle
                      size={16}
                      strokeWidth={2}
                      color={ctx.color}
                    />
                  }
                ></NavLink>
              )}
            {ctx.tables.find((table) => table.tableName === "Permission") &&
              !ctx.props.hiddenMenuItems?.includes("Permissions") && (
                <NavLink
                  label="Permissions"
                  onClick={() =>
                    ctx.setActiveComponent({
                      component: (
                        <div className="w-full h-full flex flex-col">
                          <div className="grow h-1">
                            <DataTable
                              tableName="Permission"
                              fields={["Name"]}
                              filters={[
                                {
                                  field: "Tag",
                                  operator: "!=",
                                  value: "System",
                                },
                              ]}
                              formFields={["Name"]}
                              formCloseOnCreate={true}
                              formMaxWidth={36}
                              maxResults={100}
                              formAppendButton={(formbuilder) => {
                                return (
                                  <>
                                    {formbuilder.operation === "UPDATE" && (
                                      <div className="w-full flex justify-start items-center gap-1">
                                        <CopyId
                                          value={
                                            formbuilder.data[
                                              `${formbuilder.tableName}Id`
                                            ]
                                          }
                                        />
                                      </div>
                                    )}
                                  </>
                                );
                              }}
                            />
                          </div>
                        </div>
                      ),
                    })
                  }
                  icon={<IconKey size={16} strokeWidth={2} color={ctx.color} />}
                ></NavLink>
              )}
          </NavLink>
        )}
    </>
  );
};

export default AdminDashSecurity;
