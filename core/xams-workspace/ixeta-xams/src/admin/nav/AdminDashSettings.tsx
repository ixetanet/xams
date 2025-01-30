import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconSettings } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";

const AdminDashSettings = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Setting").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Settings") && (
          <NavLink
            label="Settings"
            leftSection={
              <IconSettings size={16} strokeWidth={2} color={ctx.color} />
            }
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <DataTable
                    tableName="Setting"
                    maxResults={100}
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

export default AdminDashSettings;
