import { Button, Loader, Modal } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import React, { useMemo, useState } from "react";
import dayjs from "dayjs";
import localizedFormat from "dayjs/plugin/localizedFormat";
import { useSignalR, useSignalRResponse } from "../hooks/useSignalR";
import { useAuthContext } from "./AuthContext";
import { HubConnectionState } from "@microsoft/signalr";

export type AppContextShape = {
  showError: (message: string | React.ReactElement, title?: string) => void;
  showLoading: (text?: string) => void;
  hideLoading: () => void;
  showConfirm: (
    message: string,
    onOk: () => void,
    onCancel: () => void,
    title?: string
  ) => void;
  userId?: string | undefined;
  signalR: () => Promise<useSignalRResponse>;
  signalRState: HubConnectionState | undefined;
};

export const AppContext = React.createContext<AppContextShape | null>(null);

export const useAppContext = () => {
  const context = React.useContext(AppContext);
  if (context === null) {
    throw new Error("useAppContext must be used within a AppContextProvider");
  }
  return context;
};

interface AppContextProviderProps {
  children?: any;
}

export const AppContextProvider = (props: AppContextProviderProps) => {
  const [initialized, setInitialized] = useState(false);
  const [loadingOpened, loading] = useDisclosure(false);
  const [errorOpened, error] = useDisclosure(false);
  const [confirmOpened, confirm] = useDisclosure(false);
  const [loadingText, setLoadingText] = useState<string>("");
  const [errorTitle, setErrorTitle] = useState<string>("Error");
  const [errorMessage, setErrorMessage] = useState<string | React.ReactElement>(
    ""
  );
  const [confirmMessage, setConfirmMessage] = useState("");
  const [confirmTitle, setConfirmTitle] = useState<string>("");
  const [confirmOk, setConfirmOk] = useState<() => void>(() => () => {});
  const [confirmCancel, setConfirmCancel] = useState<() => void>(
    () => () => {}
  );
  const authContext = useAuthContext();
  const signalR = useSignalR(`${authContext.apiUrl}/xams/hub`);

  if (!initialized) {
    dayjs.extend(localizedFormat);
    setInitialized(true);
  }

  const showError = (message: string | React.ReactElement, title?: string) => {
    setErrorTitle(title ?? "Error");
    setErrorMessage(message);
    error.open();
  };

  const showConfirm = (
    message: string,
    onOk: () => void,
    onCancel: () => void,
    title?: string
  ) => {
    setConfirmMessage(message);
    setConfirmTitle(title || "Confirm");
    setConfirmOk(() => onOk);
    setConfirmCancel(() => onCancel);
    confirm.open();
  };

  const value = useMemo(
    () => ({
      showError,
      showLoading: (text?: string) => {
        setLoadingText(text || "Loading...");
        loading.open();
      },
      hideLoading: () => {
        loading.close();
      },
      showConfirm,
      signalR: () => signalR.getConnection(),
      signalRState: signalR.connectionState,
    }),
    [signalR.connectionState]
  );

  return (
    <AppContext.Provider value={value}>
      <Modal
        opened={loadingOpened}
        onClose={loading.close}
        withCloseButton={false}
        size="auto"
        closeOnClickOutside={false}
        closeOnEscape={false}
        overlayProps={{
          blur: 3,
        }}
        styles={{
          overlay: {
            zIndex: 9999,
          },
          inner: {
            zIndex: 10000,
          },
        }}
        centered
      >
        <div className="w-full h-full flex flex-col items-center gap-4 p-4">
          <div>{loadingText}</div>
          <Loader />
        </div>
      </Modal>

      <Modal
        opened={errorOpened}
        onClose={error.close}
        withCloseButton={true}
        title={errorTitle}
        size="auto"
        closeOnClickOutside={false}
        closeOnEscape={false}
        overlayProps={{
          blur: 3,
        }}
        // className=" z-50"
        styles={{
          overlay: {
            zIndex: 9999,
          },
          inner: {
            zIndex: 10000,
          },
        }}
        centered
      >
        <div className="w-full h-full flex flex-col items-center gap-4">
          <div className="whitespace-pre-wrap text-wrap">{errorMessage}</div>
          <div className="w-full flex justify-end">
            <Button onClick={error.close}>Ok</Button>
          </div>
        </div>
      </Modal>

      <Modal
        opened={confirmOpened}
        onClose={confirm.close}
        withCloseButton={true}
        title={confirmTitle}
        size="auto"
        closeOnClickOutside={false}
        closeOnEscape={false}
        overlayProps={{
          blur: 3,
        }}
        styles={{
          overlay: {
            zIndex: 9999,
          },
          inner: {
            zIndex: 10000,
          },
        }}
        centered
      >
        <div className="w-full h-full flex flex-col items-center gap-4 ">
          <div>{confirmMessage}</div>
          <div className="w-full flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                confirmCancel();
                confirm.close();
              }}
            >
              No
            </Button>
            <Button
              onClick={() => {
                confirmOk();
                confirm.close();
              }}
            >
              Yes
            </Button>
          </div>
        </div>
      </Modal>

      {/* <VDialogError
        show={isShowError}
        setShow={setIsShowError}
        message={errorMessage}
      ></VDialogError> */}

      {props.children}
    </AppContext.Provider>
  );
};
export default AppContextProvider;
