import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink } from "@mantine/core";
import { IconBox } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";

const AdminDashEntities = () => {
  const ctx = useAdminDashContext();

  return (
    <>
      {ctx.tables.length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Entities") && (
          <NavLink
            label="Entities"
            icon={<IconBox size={16} strokeWidth={2} color={ctx.color} />}
          >
            {ctx.tables
              .filter(
                (table) =>
                  table.tag !== "System" &&
                  ctx.props.hiddenEntities?.includes(table.tableName) !==
                    true &&
                  (ctx.props.visibleEntities === undefined ||
                    ctx.props.visibleEntities?.includes(table.tableName))
              )
              .sort((a, b) =>
                ctx.props.showEntityDisplayNames
                  ? a.displayName.localeCompare(b.displayName)
                  : a.tableName.localeCompare(b.tableName)
              )
              .map((table) => (
                <NavLink
                  key={table.tableName}
                  label={
                    ctx.props.showEntityDisplayNames
                      ? table.displayName
                      : table.tableName
                  }
                  icon={<IconBox size={16} strokeWidth={2} color={ctx.color} />}
                  onClick={() =>
                    ctx.setActiveComponent((prev) => {
                      return {
                        component: (
                          <div className="w-full h-full flex flex-col">
                            <div className="grow h-1">
                              <DataTable
                                tableName={table.tableName}
                                maxResults={100}
                              />
                            </div>
                          </div>
                        ),
                      };
                    })
                  }
                ></NavLink>
              ))}
          </NavLink>
        )}
    </>
  );
};

const Test = () => {
  return <div>Test</div>;
};

export default AdminDashEntities;
