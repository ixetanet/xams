import React from "react";
import { useAdminDashContext } from "../AdminDashboard";
import { NavLink, Grid, Fieldset } from "@mantine/core";
import { IconClock } from "@tabler/icons-react";
import Field from "../../components/Field";
import JobForm from "../JobForm";
import DataTable from "../../components/DataTable";
import CopyId from "../components/CopyId";
import SaveButton from "../../components/SaveButton";
import LogsViewer from "../components/logviewer/LogsViewer";

const AdminDashJobs = () => {
  const ctx = useAdminDashContext();
  return (
    <>
      {ctx.tables.filter((table) => table.tableName === "Job").length > 0 &&
        !ctx.props.hiddenMenuItems?.includes("Jobs") && (
          <NavLink
            label="Jobs"
            leftSection={
              <IconClock size={16} strokeWidth={2} color={ctx.color} />
            }
            onClick={() =>
              ctx.setActiveComponent({
                component: (
                  <DataTable
                    tableName="Job"
                    orderBy={[
                      {
                        field: "Name",
                        order: "asc",
                      },
                    ]}
                    fields={["Name", "Queue", "IsActive", "LastExecution"]}
                    filters={[
                      {
                        field: "Tag",
                        operator: "!=",
                        value: "System",
                      },
                    ]}
                    canCreate={false}
                    canDelete={false}
                    formCloseOnEscape={true}
                    customForm={(formbuilder) => {
                      return (
                        <>
                          <Grid>
                            <Grid.Col span={4}>
                              <Field name={"Name"}></Field>
                            </Grid.Col>
                            <Grid.Col span={4}>
                              <Field name={"Queue"}></Field>
                            </Grid.Col>
                            <Grid.Col span={4}>
                              <Field name={"LastExecution"}></Field>
                            </Grid.Col>
                          </Grid>
                          <Grid>
                            <Grid.Col span={4}>
                              <Field name={"IsActive"}></Field>
                            </Grid.Col>
                            <Grid.Col span={4}></Grid.Col>
                            <Grid.Col span={4}></Grid.Col>
                          </Grid>
                        </>
                      );
                    }}
                    appendCustomForm={(formbuilder) => {
                      if (formbuilder.operation === "CREATE") {
                        <></>;
                      }
                      return (
                        <>
                          <div className="w-full h-96 mt-4">
                            <DataTable
                              title="History"
                              tableName={"JobHistory"}
                              fields={[
                                "ServerName",
                                "Status",
                                "Message",
                                "CreatedDate",
                                "CompletedDate",
                              ]}
                              filters={[
                                {
                                  field: "JobId",
                                  value: formbuilder.snapshot?.JobId,
                                },
                              ]}
                              orderBy={[
                                {
                                  field: "CreatedDate",
                                  order: "desc",
                                },
                              ]}
                              maxResults={100}
                              canUpdate={false}
                              canCreate={false}
                              formFullScreen={true}
                              refreshInterval={1000}
                              formClassNames="h-full flex grow"
                              customForm={(formbuilder) => {
                                return (
                                  <div className="w-full flex flex-col gap-2">
                                    <Grid>
                                      <Grid.Col span={2.4}>
                                        <Field name={"Name"}></Field>
                                      </Grid.Col>
                                      <Grid.Col span={2.4}>
                                        <Field name={"JobId"}></Field>
                                      </Grid.Col>
                                      <Grid.Col span={2.4}>
                                        <Field name={"Status"}></Field>
                                      </Grid.Col>
                                      <Grid.Col span={2.4}>
                                        <Field name={"CreatedDate"}></Field>
                                      </Grid.Col>
                                      <Grid.Col span={2.4}>
                                        <Field name={"CompletedDate"}></Field>
                                      </Grid.Col>
                                    </Grid>
                                    <div className="w-full grow flex flex-col">
                                      <Fieldset
                                        legend="Logs"
                                        styles={{
                                          root: {
                                            flexGrow: 1,
                                          },
                                        }}
                                      >
                                        <LogsViewer
                                          jobHistoryId={
                                            formbuilder.snapshot?.JobHistoryId
                                          }
                                        />
                                      </Fieldset>
                                    </div>
                                  </div>
                                );
                              }}
                            ></DataTable>
                          </div>
                          <div className="w-full flex justify-between mt-3">
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
                            <div className="flex">
                              <div className="mr-2">
                                <JobForm formBuilder={formbuilder}></JobForm>
                              </div>
                              <SaveButton />
                            </div>
                          </div>
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

export default AdminDashJobs;
