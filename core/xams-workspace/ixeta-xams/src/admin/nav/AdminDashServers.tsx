import React from "react";
import { useAdminDashContext } from "../contexts/AdminDashContext";
import { NavLink } from "@mantine/core";
import { IconServer } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";

const AdminDashServers = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Server").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Servers") && (
          <NavLink
            label="Servers"
            leftSection={
              <IconServer size={16} strokeWidth={2} color={ctx.color} />
            }
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <DataTable
                    tableName="Server"
                    orderBy={[
                      {
                        field: "Name",
                        order: "ASC",
                      },
                    ]}
                    maxResults={100}
                    canDelete={false}
                    canCreate={false}
                    canUpdate={false}
                    refreshInterval={1000}
                    formAppendButton={(formbuilder) => {
                      return (
                        <>
                          {formbuilder.operation === "UPDATE" && (
                            <div className="w-full flex justify-start items-center gap-1">
                              <CopyId
                                value={
                                  formbuilder.data[`${formbuilder.tableName}Id`]
                                }
                              />
                            </div>
                          )}
                        </>
                      );
                    }}
                  />
                ),
              })
            }
          ></NavLink>
        )}
    </>
  );
};

export default AdminDashServers;
