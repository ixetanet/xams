import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { Button, CopyButton, NavLink } from "@mantine/core";
import { IconBox, IconClipboard, IconCheck } from "@tabler/icons-react";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";

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
                          <DataTable
                            tableName={table.tableName}
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
