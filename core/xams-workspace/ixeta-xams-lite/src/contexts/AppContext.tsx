"use client";

import { ReactNode, ReactElement, useMemo, createContext, useContext } from "react";
import { useSignalR, useSignalRResponse } from "../hooks/useSignalR";
import { useAuthContext } from "./AuthContext";
import { HubConnectionState } from "@microsoft/signalr";

export type AppContextShape = {
  userId?: string | undefined;
  signalR: () => Promise<useSignalRResponse>;
  signalRState: HubConnectionState | undefined;
  showError: (message: string | ReactElement, title?: string) => void;
};

export const AppContext = createContext<AppContextShape | null>(null);

export const useAppContext = () => {
  const context = useContext(AppContext);
  if (context === null) {
    throw new Error("useAppContext must be used within a AppContextProvider");
  }
  return context;
};

interface AppContextProviderProps {
  onError?: (message: string | ReactElement, title?: string) => void;
  children?: ReactNode;
}

export const AppContextProvider = (props: AppContextProviderProps) => {
  const authContext = useAuthContext();
  const signalR = useSignalR(`${authContext.apiUrl}/xams/hub`);

  const showError = (message: string | ReactElement, title?: string) => {
    if (props.onError) {
      props.onError(message, title);
    } else {
      console.error(title ? `${title} - ${message}` : message);
    }
  };

  const value = useMemo(
    () => ({
      signalR: () => signalR.getConnection(),
      signalRState: signalR.connectionState,
      showError,
    }),
    [signalR.connectionState, showError]
  );

  return (
    <AppContext.Provider value={value}>{props.children}</AppContext.Provider>
  );
};
export default AppContextProvider;
