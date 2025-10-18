import React, { useState } from "react";
import { useAdminDashContext } from "../contexts/AdminDashContext";
import { Grid, NavLink, Tabs } from "@mantine/core";
import { IconReportSearch } from "@tabler/icons-react";
import AuditForm from "../components/AuditForm";
import AuditHistoryForm from "../components/AuditHistoryForm";
import DataTable from "../../components/DataTable";
import Field from "../../components/Field";
import AuditHistoryDetailForm from "../components/AuditHistoryDetailForm";

const AdminDashAudit = () => {
  const ctx = useAdminDashContext();

  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Option").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Options") && (
          <NavLink
            label="Audit"
            leftSection={
              <IconReportSearch size={16} strokeWidth={2} color={ctx.color} />
            }
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <Tabs
                    defaultValue="history"
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
                      <Tabs.Tab value="history">History</Tabs.Tab>
                      <Tabs.Tab value="field-history">Field History</Tabs.Tab>
                      <Tabs.Tab value="config">Config</Tabs.Tab>
                    </Tabs.List>

                    <Tabs.Panel value="history">
                      <div className="w-full h-full pt-4">
                        <DataTable
                          tableName="AuditHistory"
                          fields={[
                            "Operation",
                            "TableName",
                            "User",
                            "Name",
                            "CreatedDate",
                            "EntityId",
                          ]}
                          columnWidths={[
                            "125px",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                          ]}
                          title="History"
                          maxResults={100}
                          canCreate={false}
                          orderBy={[
                            {
                              field: "CreatedDate",
                              order: "desc",
                            },
                          ]}
                          customForm={(formBuilder) => (
                            <AuditHistoryForm formBuilder={formBuilder} />
                          )}
                        />
                      </div>
                    </Tabs.Panel>

                    <Tabs.Panel value="field-history">
                      <div className="w-full h-full pt-4">
                        <DataTable
                          tableName="AuditHistoryDetail"
                          fields={[
                            "ah.Operation",
                            "ah.Name",
                            "ah.TableName",
                            "FieldName",
                            "OldValue",
                            "NewValue",
                            "ah.CreatedDate",
                            "ah.EntityId",
                          ]}
                          columnWidths={[
                            "125px",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                            "100px",
                          ]}
                          title="Field History"
                          maxResults={100}
                          canCreate={false}
                          canUpdate={false}
                          canDelete={false}
                          joins={[
                            {
                              fromTable: "AuditHistoryDetail",
                              fromField: "AuditHistoryId",
                              toTable: "AuditHistory",
                              toField: "AuditHistoryId",
                              fields: [
                                "EntityId",
                                "TableName",
                                "Name",
                                "CreatedDate",
                                "Operation",
                              ],
                              alias: "ah",
                            },
                          ]}
                          orderBy={[
                            {
                              field: "ah.CreatedDate",
                              order: "desc",
                            },
                          ]}
                          customForm={(formBuilder) => (
                            <AuditHistoryDetailForm />
                          )}
                        />
                      </div>
                    </Tabs.Panel>

                    <Tabs.Panel value="config">
                      <div className="w-full h-full pt-4">
                        <DataTable
                          tableName="Audit"
                          title="Audit"
                          formTitle="Audit"
                          maxResults={100}
                          orderBy={[
                            {
                              field: "Name",
                              order: "asc",
                            },
                          ]}
                          fields={[
                            "Name",
                            "IsCreate",
                            "IsRead",
                            "IsUpdate",
                            "IsDelete",
                          ]}
                          columnWidths={[
                            "350px",
                            "100%",
                            "100%",
                            "100%",
                            "100%",
                          ]}
                          canCreate={false}
                          canDelete={false}
                          formMaxWidth={55}
                          formCloseOnEscape={true}
                          customForm={(formBuilder) => (
                            <AuditForm formBuilder={formBuilder} />
                          )}
                        />
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

export default AdminDashAudit;
