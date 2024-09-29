import { Grid, Tabs, Checkbox, Alert } from "@mantine/core";
import React, { useRef, useState } from "react";
import { AuditField } from "../../api/AuditField";
import DataTable from "../../components/DataTableImp";
import Field from "../../components/Field";
import SaveButton from "../../components/SaveButton";
import useAuthRequest from "../../hooks/useAuthRequest";
import { useFormBuilderType } from "../../hooks/useFormBuilder";
import { DataTableRef } from "../../components/datatable/DataTableTypes";
import { IconAlertCircle } from "@tabler/icons-react";

interface AuditFieldDataTableProps {
  formBuilder: useFormBuilderType;
}

const AuditForm = (props: AuditFieldDataTableProps) => {
  const authRequest = useAuthRequest();
  const dataTableRef = useRef<DataTableRef>(null);
  const [isCreateAll, setIsCreateAll] = useState<boolean>(false);
  const [isUpdateAll, setIsUpdateAll] = useState<boolean>(false);
  const [isDeleteAll, setIsDeleteAll] = useState<boolean>(false);

  const getAllSelected = (data: AuditField[]) => {
    data.filter((field) => field.IsCreate).length === data.length
      ? setIsCreateAll(true)
      : setIsCreateAll(false);
    data.filter((field) => field.IsUpdate).length === data.length
      ? setIsUpdateAll(true)
      : setIsUpdateAll(false);
    data.filter((field) => field.IsDelete).length === data.length
      ? setIsDeleteAll(true)
      : setIsDeleteAll(false);
  };

  const updateAuditFields = async (data: AuditField[]) => {
    const resp = await authRequest.bulkUpdate(
      data.map((d) => {
        return {
          tableName: "AuditField",
          fields: {
            ...d,
          },
        };
      })
    );
    if (resp.succeeded === true) {
      return true;
    }
    dataTableRef.current?.refresh();
    return false;
  };

  const updateAuditField = async (data: AuditField) => {
    const resp = await authRequest.update("AuditField", data);
    if (resp.succeeded === true) {
      return true;
    }
    dataTableRef.current?.refresh();
    return false;
  };

  return (
    <div className="w-full flex flex-col gap-4">
      <Alert
        icon={<IconAlertCircle size="1rem" />}
        // title="Notice"
        color="yellow"
      >
        In a multi-server environment, Audit configuration changes may take up
        to 5 minutes to take effect.
      </Alert>
      {props.formBuilder.snapshot?.IsTable && (
        <Grid>
          <Grid.Col span={4}>
            <Field name="Name" />
          </Grid.Col>
          <Grid.Col span={2}>
            <Field name="IsCreate" />
          </Grid.Col>
          <Grid.Col span={2}>
            <Field name="IsRead" />
          </Grid.Col>
          <Grid.Col span={2}>
            <Field name="IsUpdate" />
          </Grid.Col>
          <Grid.Col span={2}>
            <Field name="IsDelete" />
          </Grid.Col>
        </Grid>
      )}
      <div className="w-full h-[550px]">
        <DataTable
          tableName="AuditField"
          columnWidths={["100%", "130px", "130px", "130px"]}
          additionalFields={["IsCreate", "IsUpdate", "IsDelete"]}
          canDelete={false}
          fields={[
            "Name",
            {
              header: (ref) => (
                <div>
                  <Checkbox
                    label="Create"
                    checked={isCreateAll}
                    onChange={(e) => {
                      setIsCreateAll(e.target.checked);
                      ref.setRecords((prev) => {
                        const auditFields = prev.map((r) => {
                          return {
                            ...r,
                            IsCreate: e.target.checked,
                          };
                        });
                        updateAuditFields(auditFields);
                        return auditFields;
                      });
                    }}
                  ></Checkbox>
                </div>
              ),

              body: (data, ref) => (
                <Checkbox
                  checked={data.IsCreate}
                  onChange={(e) => {
                    ref.setRecords((prev) => {
                      const updatedRecords = prev.map((r) => {
                        return r.AuditFieldId === data.AuditFieldId
                          ? {
                              ...r,
                              IsCreate: e.target.checked,
                            }
                          : r;
                      });
                      updatedRecords.filter((r) => r.IsCreate).length ===
                      prev.length
                        ? setIsCreateAll(true)
                        : setIsCreateAll(false);
                      updateAuditField(
                        updatedRecords.find(
                          (r) => r.AuditFieldId === data.AuditFieldId
                        ) as AuditField
                      );
                      return updatedRecords;
                    });
                  }}
                ></Checkbox>
              ),
            },
            {
              header: (ref) => (
                <div>
                  <Checkbox
                    label="Update"
                    checked={isUpdateAll}
                    onChange={(e) => {
                      setIsUpdateAll(e.target.checked);
                      ref.setRecords((prev) => {
                        const auditFields = prev.map((r) => {
                          return {
                            ...r,
                            IsUpdate: e.target.checked,
                          };
                        });
                        updateAuditFields(auditFields);
                        return auditFields;
                      });
                    }}
                  ></Checkbox>
                </div>
              ),

              body: (data, ref) => (
                <Checkbox
                  checked={data.IsUpdate}
                  onChange={(e) => {
                    ref.setRecords((prev) => {
                      const updatedRecords = prev.map((r) => {
                        return r.AuditFieldId === data.AuditFieldId
                          ? {
                              ...r,
                              IsUpdate: e.target.checked,
                            }
                          : r;
                      });
                      updatedRecords.filter((r) => r.IsUpdate).length ===
                      prev.length
                        ? setIsUpdateAll(true)
                        : setIsUpdateAll(false);
                      updateAuditField(
                        updatedRecords.find(
                          (r) => r.AuditFieldId === data.AuditFieldId
                        ) as AuditField
                      );
                      return updatedRecords;
                    });
                  }}
                ></Checkbox>
              ),
            },
            {
              header: (ref) => (
                <div>
                  <Checkbox
                    label="Delete"
                    checked={isDeleteAll}
                    onChange={(e) => {
                      setIsDeleteAll(e.target.checked);
                      ref.setRecords((prev) => {
                        const auditFields = prev.map((r) => {
                          return {
                            ...r,
                            IsDelete: e.target.checked,
                          };
                        });
                        updateAuditFields(auditFields);
                        return auditFields;
                      });
                    }}
                  ></Checkbox>
                </div>
              ),

              body: (data, ref) => (
                <Checkbox
                  checked={data.IsDelete}
                  onChange={(e) => {
                    ref.setRecords((prev) => {
                      const updatedRecords = prev.map((r) => {
                        return r.AuditFieldId === data.AuditFieldId
                          ? {
                              ...r,
                              IsDelete: e.target.checked,
                            }
                          : r;
                      });
                      updatedRecords.filter((r) => r.IsDelete).length ===
                      prev.length
                        ? setIsDeleteAll(true)
                        : setIsDeleteAll(false);
                      updateAuditField(
                        updatedRecords.find(
                          (r) => r.AuditFieldId === data.AuditFieldId
                        ) as AuditField
                      );
                      return updatedRecords;
                    });
                  }}
                ></Checkbox>
              ),
            },
          ]}
          maxResults={99999}
          onRowClick={(row) => {
            return false;
          }}
          filters={[
            {
              field: "AuditId",
              operator: "==",
              value: props.formBuilder.snapshot?.AuditId,
            },
          ]}
          orderBy={[
            {
              field: "Name",
              order: "asc",
            },
          ]}
          onDataLoaded={(data) => {
            getAllSelected(data.results as AuditField[]);
          }}
        ></DataTable>
      </div>
      <div className="w-full flex justify-end">
        <SaveButton />
      </div>
    </div>
  );
};

export default AuditForm;
