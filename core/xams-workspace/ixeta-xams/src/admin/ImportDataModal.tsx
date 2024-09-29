import { API_DATA_FILE } from "../apiurls";
import useAuthRequest from "../hooks/useAuthRequest";
import { Button, FileInput, Loader, Modal } from "@mantine/core";
import React, { useEffect } from "react";

interface ImportDataModalProps {
  opened: boolean;
  close: () => void;
}

const ImportDataModal = (props: ImportDataModalProps) => {
  const authRequest = useAuthRequest();
  const [currentFile, setCurrentFile] = React.useState<File | null>(null);
  const [isLoading, setIsLoading] = React.useState<boolean>(false);

  const onUpload = async () => {
    if (currentFile === null) return;
    setIsLoading(true);
    const formData = new FormData();
    formData.append("file", currentFile);
    formData.append("name", "ADMIN_ImportData");
    const resp = await authRequest.execute({
      url: API_DATA_FILE,
      method: "POST",
      body: formData,
    });
    props.close();
  };

  useEffect(() => {
    if (props.opened === true) {
      setIsLoading(false);
      setCurrentFile(null);
    }
  }, [props.opened]);

  return (
    <Modal
      opened={props.opened}
      onClose={props.close}
      size="md"
      title="Import Data"
      closeOnEscape={isLoading ? false : true}
      closeOnClickOutside={isLoading ? false : true}
      withCloseButton={!isLoading}
      styles={{
        body: {
          overflow: "hidden",
          position: "relative",
          padding: "0px",
        },
      }}
      centered
    >
      {isLoading === true && (
        <div className="absolute w-full h-full  flex justify-center items-center overflow-hidden z-30">
          <Loader />
        </div>
      )}
      <div className={`p-4 ${isLoading ? `invisible` : ``}`}>
        <FileInput
          label="File Upload"
          placeholder="Click to choose data file"
          required
          value={currentFile}
          onChange={(file) => {
            setCurrentFile(file);
          }}
        />
        <div className="w-full flex justify-end mt-4">
          <Button onClick={onUpload} disabled={currentFile === null}>
            Upload
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ImportDataModal;
