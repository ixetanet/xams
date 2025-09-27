import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconLogs } from "@tabler/icons-react";
import LogsViewer from "../components/logviewer/LogsViewer";

const AdminDashLogs = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {!ctx.props.hiddenMenuItems?.includes("Logs") && (
        <NavLink
          label="Logs"
          leftSection={<IconLogs size={16} strokeWidth={2} color={ctx.color} />}
          onClick={() =>
            ctx.setActiveComponent({
              component: <LogsViewer />,
            })
          }
        ></NavLink>
      )}
    </>
  );
};

export default AdminDashLogs;
