import React, { createContext, useContext, useState, ReactNode } from "react";
import { useRouter } from "next/router";
import { useAuth } from "@ixeta/headless-auth-react";
import { useAuthType } from "@ixeta/headless-auth-react/src/useAuth";

interface LoginContextType {
  auth: ReturnType<typeof useAuth>;
  router: ReturnType<typeof useRouter>;
  providers: string[];
  loadingStates: Record<string, boolean>;
  setLoadingStates: React.Dispatch<
    React.SetStateAction<Record<string, boolean>>
  >;
  getProviderDisplayName: (provider: string) => string;
  getProviderIcon: (provider: string) => React.ReactNode;
  isAnyProviderLoading: () => boolean;
  deactivateModal: {
    isOpen: boolean;
    type: 'totp' | 'sms' | null;
  };
  setDeactivateModal: React.Dispatch<
    React.SetStateAction<{
      isOpen: boolean;
      type: 'totp' | 'sms' | null;
    }>
  >;
}

const LoginContext = createContext<LoginContextType | undefined>(undefined);

interface LoginProviderProps {
  auth: useAuthType;
  children: ReactNode;
  providers: string[];
}

export const LoginProvider = ({
  children,
  providers,
  auth,
}: LoginProviderProps) => {
  const router = useRouter();

  const getProviderDisplayName = (provider: string): string => {
    const displayNames: Record<string, string> = {
      google: "Google",
      facebook: "Facebook",
      twitter: "Twitter",
      github: "GitHub",
      apple: "Apple",
      microsoft: "Microsoft",
      yahoo: "Yahoo",
    };
    return displayNames[provider] || provider;
  };

  const getProviderIcon = (provider: string): React.ReactNode => {
    switch (provider) {
      case "google":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path
              fill="#4285F4"
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
            />
            <path
              fill="#34A853"
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
            />
            <path
              fill="#FBBC05"
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
            />
            <path
              fill="#EA4335"
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
            />
          </svg>
        );
      case "microsoft":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#f25022" d="M1 1h10v10H1z" />
            <path fill="#00a4ef" d="M13 1h10v10H13z" />
            <path fill="#7fba00" d="M1 13h10v10H1z" />
            <path fill="#ffb900" d="M13 13h10v10H13z" />
          </svg>
        );
      case "facebook":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path
              fill="#1877f2"
              d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"
            />
          </svg>
        );
      case "github":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path
              fill="#333333"
              d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
            />
          </svg>
        );
      case "apple":
        return (
          <svg width="20" height="20" viewBox="0 0 814 1000">
            <path
              fill="#000000"
              d="M788.1 340.9c-5.8 4.5-108.2 62.2-108.2 190.5 0 148.4 130.3 200.9 134.2 202.2-.6 3.2-20.7 71.9-68.7 141.9-42.8 61.6-87.5 123.1-155.5 123.1s-85.5-39.5-164-39.5c-76.5 0-103.7 40.8-165.9 40.8s-105.6-57-155.5-127C46.7 790.7 0 663 0 541.8c0-194.4 126.4-297.5 250.8-297.5 66.1 0 121.2 43.4 162.7 43.4 39.5 0 101.1-46 176.3-46 28.5 0 130.9 2.6 198.3 99.2zm-234-181.5c31.1-36.9 53.1-88.1 53.1-139.3 0-7.1-.6-14.3-1.9-20.1-50.6 1.9-110.8 33.7-147.1 75.8-28.5 32.4-55.1 83.6-55.1 135.5 0 7.8 1.3 15.6 1.9 18.1 3.2.6 8.4 1.3 13.6 1.3 45.4 0 102.5-30.4 135.5-71.3z"
            />
          </svg>
        );
      case "twitter":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path
              fill="#1DA1F2"
              d="M23.953 4.57a10 10 0 01-2.825.775 4.958 4.958 0 002.163-2.723c-.951.555-2.005.959-3.127 1.184a4.92 4.92 0 00-8.384 4.482C7.69 8.095 4.067 6.13 1.64 3.162a4.822 4.822 0 00-.666 2.475c0 1.71.87 3.213 2.188 4.096a4.904 4.904 0 01-2.228-.616v.06a4.923 4.923 0 003.946 4.827 4.996 4.996 0 01-2.212.085 4.936 4.936 0 004.604 3.417 9.867 9.867 0 01-6.102 2.105c-.39 0-.779-.023-1.17-.067a13.995 13.995 0 007.557 2.209c9.053 0 13.998-7.496 13.998-13.985 0-.21 0-.42-.015-.63A9.935 9.935 0 0024 4.59z"
            />
          </svg>
        );
      case "yahoo":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path
              fill="#410093"
              d="M12 0C5.383 0 0 5.383 0 12s5.383 12 12 12 12-5.383 12-12S18.617 0 12 0zm6.281 19.032H15.4l-2.044-8.863-2.044 8.863H8.431l3.588-14.024h1.875l3.387 14.024z"
            />
          </svg>
        );
      default:
        return null;
    }
  };

  const createProviderLoadingStates = () => {
    const providerStates: Record<string, boolean> = {};
    providers.forEach((provider) => {
      providerStates[`${provider}SignIn`] = false;
    });
    return providerStates;
  };

  const [loadingStates, setLoadingStates] = useState<Record<string, boolean>>({
    signIn: false,
    signUp: false,
    mfaTotp: false,
    mfaSms: false,
    resendEmail: false,
    totpCreate: false,
    totpEnroll: false,
    totpUnenroll: false,
    smsCreate: false,
    smsEnroll: false,
    smsUnenroll: false,
    ...createProviderLoadingStates(),
  });

  const [deactivateModal, setDeactivateModal] = useState<{
    isOpen: boolean;
    type: 'totp' | 'sms' | null;
  }>({
    isOpen: false,
    type: null,
  });

  const isAnyProviderLoading = () => {
    return providers.some(
      (provider) => loadingStates[`${provider}SignIn`] || false
    );
  };

  const value: LoginContextType = {
    auth,
    router,
    providers,
    loadingStates,
    setLoadingStates,
    getProviderDisplayName,
    getProviderIcon,
    isAnyProviderLoading,
    deactivateModal,
    setDeactivateModal,
  };

  return (
    <LoginContext.Provider value={value}>{children}</LoginContext.Provider>
  );
};

export const useLoginContext = () => {
  const context = useContext(LoginContext);
  if (context === undefined) {
    throw new Error("useLoginContext must be used within a LoginProvider");
  }
  return context;
};
