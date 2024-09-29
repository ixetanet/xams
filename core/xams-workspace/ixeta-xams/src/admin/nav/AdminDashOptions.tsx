import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconSelector } from "@tabler/icons-react";
import { DataTable } from "../..";

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
                  <div className="w-full h-full flex flex-col">
                    <div className="grow h-1">
                      <DataTable tableName="Option" maxResults={100} />
                    </div>
                  </div>
                ),
              })
            }
          ></NavLink>
        )}
    </>
  );
};

export default AdminDashOptions;
