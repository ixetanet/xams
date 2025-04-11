import React, { useMemo } from "react";

export interface AuthContextProviderProps {
  onUnauthorized?: () => void;
  apiUrl: string;
  headers?: { [key: string]: string };
  children?: any;
  withCredentials?: boolean;
}

export type AuthContextShape = {
  onUnauthorized?: () => void;
  apiUrl: string;
  headers?: { [key: string]: string };
  withCredentials?: boolean;
};

export const AuthContext = React.createContext<AuthContextShape | null>(null);

export const useAuthContext = () => {
  const ctx = React.useContext(AuthContext);
  if (!ctx) {
    throw new Error(
      "useAuthContext must be used within an AuthContextProvider"
    );
  }
  return ctx;
};

const AuthContextProvider = (props: AuthContextProviderProps) => {
  // Use this for memoization, otherwise the context will be recreated on every render
  let headersString = "";
  if (props.headers != null) {
    for (const key in props.headers) {
      headersString += `${key}: ${props.headers[key]}\n`;
    }
  }

  return (
    <AuthContext.Provider
      value={{
        onUnauthorized: props.onUnauthorized,
        apiUrl: props.apiUrl,
        headers: props.headers,
        withCredentials: props.withCredentials,
      }}
    >
      {props.children}
    </AuthContext.Provider>
  );
};

export default AuthContextProvider;
