import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconSettings } from "@tabler/icons-react";
import DataTable from "../../components/DataTableImp";

const AdminDashSettings = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Setting").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Settings") && (
          <NavLink
            label="Settings"
            icon={<IconSettings size={16} strokeWidth={2} color={ctx.color} />}
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <div className="w-full h-full flex flex-col">
                    <div className="grow h-1">
                      <DataTable tableName="Setting" maxResults={100} />
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

export default AdminDashSettings;
