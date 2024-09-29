import { Button, FileInput, Loader, Modal, Select } from "@mantine/core";
import React, { useEffect } from "react";
import useAuthRequest from "../../hooks/useAuthRequest";
import { API_DATA_FILE } from "../../apiurls";
import { useDataTableContext } from "../DataTableImp";

interface ImportDataProps {
  opened: boolean;
  close: () => void;
}

const DataTableImportData = (props: ImportDataProps) => {
  const authRequest = useAuthRequest();
  const ctx = useDataTableContext();
  const [currentFile, setCurrentFile] = React.useState<File | null>(null);
  const [isLoading, setIsLoading] = React.useState<boolean>(false);
  const [operation, setOperation] = React.useState<string>("create,update");
  const [errors, setErrors] = React.useState<string[]>([]);

  const onUpload = async () => {
    if (currentFile === null) return;
    setIsLoading(true);
    const formData = new FormData();
    formData.append("file", currentFile);
    formData.append("name", "TABLE_ImportData");
    formData.append(
      "parameters",
      JSON.stringify({ tableName: ctx.props.tableName, operation: operation })
    );
    const resp = await authRequest.execute({
      url: API_DATA_FILE,
      method: "POST",
      body: formData,
      hideFailureMessage: true,
    });
    if (resp?.succeeded === false) {
      const data = resp.data as { errors: string[] };
      if (data != null && data.errors != null && data.errors.length > 0) {
        setErrors(data.errors);
      } else {
        setErrors([resp.friendlyMessage ?? "Unknown error"]);
      }
    } else if (resp?.succeeded === true) {
      ctx.refresh();
      props.close();
    }
    setIsLoading(false);
  };

  useEffect(() => {
    if (props.opened === true) {
      setIsLoading(false);
      setCurrentFile(null);
      setErrors([]);
    }
  }, [props.opened]);

  return (
    <Modal
      opened={props.opened}
      onClose={props.close}
      title="Import Data"
      size="lg"
      closeOnEscape={!isLoading}
      closeOnClickOutside={!isLoading}
      withCloseButton={!isLoading}
      centered
      styles={{
        overlay: {
          zIndex: 4000,
        },
        inner: {
          zIndex: 4001,
        },
      }}
    >
      {errors.length > 0 ? (
        <div className="w-full flex flex-col gap-4 h-96">
          <div className="w-full h-full flex flex-col gap-2 overflow-y-auto">
            {errors.map((e, i) => (
              <div key={i} className="w-full bg-red-100 py-2 rounded-md">
                {e}
              </div>
            ))}
          </div>
        </div>
      ) : (
        <>
          <div className={`w-full relative`}>
            {isLoading === true && (
              <div className="absolute w-full h-full flex justify-center items-center">
                <Loader />
              </div>
            )}
            <div
              className={`w-full flex flex-col gap-4 ${
                isLoading ? `invisible` : ``
              }`}
            >
              <Select
                label="Operation"
                placeholder="Select Operation"
                data={[
                  { value: "create,update", label: "Create and Update" },
                  { value: "create", label: "Create Only" },
                  { value: "update", label: "Update Only" },
                ]}
                value={operation}
                onChange={(value) => {
                  setOperation(value as string);
                }}
              />
              <FileInput
                placeholder="Click to choose import file"
                label="File Upload"
                required
                value={currentFile}
                onChange={(file) => {
                  setCurrentFile(file);
                }}
              />
              <div className="w-full flex justify-end">
                <Button onClick={onUpload} disabled={currentFile === null}>
                  Upload
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </Modal>
  );
};

export default DataTableImportData;
