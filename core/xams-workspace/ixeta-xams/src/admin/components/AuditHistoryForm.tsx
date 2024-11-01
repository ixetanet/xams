import React, { useEffect } from "react";
import { useFormBuilderType } from "../../hooks/useFormBuilder";
import { Grid, TextInput } from "@mantine/core";
import Field from "../../components/Field";
import DataTable from "../../components/DataTableImp";
import CopyId from "./CopyId";

interface AuditHistoryFormProps {
  formBuilder: useFormBuilderType;
}

const AuditHistoryForm = (props: AuditHistoryFormProps) => {
  // Beautify Json on load
  useEffect(() => {
    if (props.formBuilder.snapshot?.Operation !== "Read") return;
    const queryString = JSON.stringify(
      JSON.parse(props.formBuilder.snapshot?.Query),
      null,
      4
    );
    const resultsString = JSON.stringify(
      JSON.parse(props.formBuilder.snapshot?.Results),
      null,
      4
    );
    props.formBuilder.setField("Query", queryString);
    props.formBuilder.setField("Results", resultsString);
  }, [props.formBuilder.snapshot?.Query, props.formBuilder.snapshot?.Results]);
  return (
    <div className="w-full flex flex-col gap-4">
      <Grid>
        <Grid.Col span={4}>
          <Field name="TableName" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="Operation" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="CreatedDate" />
        </Grid.Col>
      </Grid>
      <Grid>
        <Grid.Col span={4}>
          <Field name="UserId" />
        </Grid.Col>
        <Grid.Col span={4}>
          <TextInput
            label="UserId"
            value={props.formBuilder.snapshot?.UserId}
            readOnly
          ></TextInput>
        </Grid.Col>
      </Grid>
      <Grid>
        {props.formBuilder.snapshot?.Operation !== "Read" && (
          <>
            <Grid.Col span={4}>
              <Field name="Name" />
            </Grid.Col>
            <Grid.Col span={4}>
              <Field name="EntityId" />
            </Grid.Col>
          </>
        )}
      </Grid>
      {props.formBuilder.snapshot?.Operation === "Read" && (
        <Grid>
          <Grid.Col span={6}>
            <div className="w-full h-[60vh]">
              <Field name="Query" varient="textarea" />
            </div>
          </Grid.Col>
          <Grid.Col span={6}>
            <div className="w-full h-[60vh]">
              <Field name="Results" varient="textarea" />
            </div>
          </Grid.Col>
        </Grid>
      )}
      {props.formBuilder.snapshot?.Operation !== "Read" && (
        <div className="w-full h-[500px] mt-2">
          <DataTable
            tableName="AuditHistoryDetail"
            disabledMessage={
              props.formBuilder.snapshot == null ? "Loading..." : undefined
            }
            fields={["Name", "OldValue", "NewValue", "EntityName"]}
            maxResults={100}
            orderBy={[
              {
                field: "Name",
                order: "ASC",
              },
            ]}
            filters={[
              {
                field: "AuditHistoryId",
                value: props.formBuilder.snapshot?.AuditHistoryId,
              },
            ]}
          />
        </div>
      )}
      {props.formBuilder.operation === "UPDATE" && (
        <div className="w-full flex justify-start items-center gap-1">
          <CopyId
            value={props.formBuilder.data[`${props.formBuilder.tableName}Id`]}
          />
        </div>
      )}
    </div>
  );
};

export default AuditHistoryForm;
