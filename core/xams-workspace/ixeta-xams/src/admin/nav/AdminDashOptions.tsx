import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconSelector } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";

const AdminDashOptions = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Option").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Options") && (
          <NavLink
            label="Options"
            icon={<IconSelector size={16} strokeWidth={2} color={ctx.color} />}
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <DataTable
                    tableName="Option"
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

export default AdminDashOptions;
