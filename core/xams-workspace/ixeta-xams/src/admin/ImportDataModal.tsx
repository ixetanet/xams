import { API_DATA_FILE } from "../apiurls";
import useAuthRequest from "../hooks/useAuthRequest";
import { Button, FileInput, Loader, Modal } from "@mantine/core";
import React, { useEffect, useRef, useState } from "react";

interface ImportDataModalProps {
  opened: boolean;
  close: () => void;
}

const ImportDataModal = (props: ImportDataModalProps) => {
  const authRequest = useAuthRequest();
  const [currentFile, setCurrentFile] = React.useState<File | null>(null);
  const [isLoading, setIsLoading] = React.useState<boolean>(false);
  const jobHistoryId = useRef<string | null>(null);
  const isCheckingStatus = useRef<boolean>(false);
  const interval = useRef<number | null>(null);

  const checkJobStatus = async () => {
    if (isCheckingStatus.current) return;
    isCheckingStatus.current = true;
    try {
      const resp = await authRequest.action<any>("ADMIN_ImportData", {
        jobHistoryId: jobHistoryId.current,
      });
      if (!resp.succeeded) {
        // if there was an error, keep trying
        isCheckingStatus.current = false;
        return;
      }
      const status = resp.data.jobStatus;
      if (status !== "Running" && status !== "Not Started") {
        clearInterval(interval.current as number);
        interval.current = null;
        setIsLoading(false);
        if (status === "Failed") {
          console.error("Job failed", resp.data.jobMessage);
          // Handle job failure
        } else if (status === "Completed") {
          console.log("Job completed successfully");
          props.close();
        }
      }
      console.log(status);
    } catch (error) {
      // if there was an error, keep trying
      isCheckingStatus.current = false;
      return;
    }

    isCheckingStatus.current = false;
  };

  const onUpload = async () => {
    if (currentFile === null) return;
    setIsLoading(true);
    const formData = new FormData();
    formData.append("file", currentFile);
    formData.append("name", "ADMIN_ImportData");
    const resp = await authRequest.execute<any>({
      url: API_DATA_FILE,
      method: "POST",
      body: formData,
    });
    if (resp.succeeded) {
      jobHistoryId.current = resp.data.jobHistoryId;
      interval.current = setInterval(checkJobStatus, 2000) as unknown as number;
    }
    // props.close();
  };

  useEffect(() => {
    if (props.opened === true) {
      setIsLoading(false);
      setCurrentFile(null);
      jobHistoryId.current = null;
      if (interval.current != null) {
        clearInterval(interval.current);
      }
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
