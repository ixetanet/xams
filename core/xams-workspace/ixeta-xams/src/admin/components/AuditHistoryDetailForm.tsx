import React from "react";
import { useFormBuilderType } from "../../hooks/useFormBuilder";
import { Grid } from "@mantine/core";
import Field from "../../components/Field";

const AuditHistoryDetailForm = () => {
  return (
    <div className="w-full">
      <Grid>
        <Grid.Col span={4}>
          <Field name="TableName" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="FieldName" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="FieldType" />
        </Grid.Col>
      </Grid>
      <Grid>
        <Grid.Col span={4}>
          <Field name="OldValue" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="NewValue" />
        </Grid.Col>
        <Grid.Col span={4}></Grid.Col>
      </Grid>
      <Grid>
        <Grid.Col span={4}>
          <Field name="OldValueId" />
        </Grid.Col>
        <Grid.Col span={4}>
          <Field name="NewValueId" />
        </Grid.Col>
        <Grid.Col span={4}></Grid.Col>
      </Grid>
    </div>
  );
};

export default AuditHistoryDetailForm;
