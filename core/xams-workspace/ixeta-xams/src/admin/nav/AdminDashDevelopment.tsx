import React from "react";
import { useAdminDashContext } from "../contexts/AdminDashContext";
import { NavLink, Tabs, Textarea } from "@mantine/core";
import { IconHammer } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";
import TypescriptTypes from "../components/TypescriptTypes";

const AdminDashDevelopment = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {!ctx.props.hiddenMenuItems?.includes("Development") && (
        <NavLink
          label="Development"
          leftSection={
            <IconHammer size={16} strokeWidth={2} color={ctx.color} />
          }
          onClick={() =>
            ctx.setActiveComponent({
              component: (
                <Tabs
                  defaultValue="types"
                  styles={{
                    root: {
                      display: "flex",
                      flexDirection: "column",
                      height: "100%",
                      flexGrow: 1,
                    },
                    panel: {
                      display: "flex",
                      flexDirection: "column",
                      flexGrow: 1,
                      height: 1,
                    },
                  }}
                >
                  <Tabs.List>
                    <Tabs.Tab value="types">Types</Tabs.Tab>
                  </Tabs.List>

                  <Tabs.Panel value="types">
                    <div className="w-full h-full pt-4">
                      <TypescriptTypes />
                    </div>
                  </Tabs.Panel>
                </Tabs>
              ),
            })
          }
        ></NavLink>
      )}
    </>
  );
};

export default AdminDashDevelopment;
