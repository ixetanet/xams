import { useRef } from "react";
import {
  useFormBuilder,
  Field,
  FormContainer,
  SaveButton,
  DataTable,
  DataTableRef,
  ToggleMode,
} from "@ixeta/xams";
import { Grid } from "@mantine/core";

export default function Home() {
  const dataTableRef = useRef<DataTableRef>(null);
  const formBuilder = useFormBuilder({
    tableName: "User",
    onPostSave: async () => {
      dataTableRef.current?.refresh();
    },
  });

  return (
    <div className="w-full h-full flex flex-col p-6 gap-4">
      <div className="flex justify-end">
        <ToggleMode />
      </div>
      <FormContainer formBuilder={formBuilder}>
        <div className="w-full">
          <Grid>
            <Grid.Col span={6}>
              <Field name="Name" />
            </Grid.Col>
            <Grid.Col span={6}>
              <Field name="CreatedDate" />
            </Grid.Col>
          </Grid>
          <div className="w-full flex flex-col pt-4">
            <SaveButton />
          </div>
        </div>
      </FormContainer>
      <div className="grow pt-6">
        <DataTable ref={dataTableRef} tableName="User"></DataTable>
      </div>
    </div>
  );
}
