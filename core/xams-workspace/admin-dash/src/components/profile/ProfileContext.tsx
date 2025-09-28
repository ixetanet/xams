import React, { createContext, useContext, useState, ReactNode } from "react";
import { useRouter } from "next/router";
import { useAuth } from "@ixeta/headless-auth-react";
import { useAuthType } from "@ixeta/headless-auth-react/src/useAuth";

interface ProfileContextType {
  auth: ReturnType<typeof useAuth>;
  router: ReturnType<typeof useRouter>;
  providers: string[];
  loadingStates: {
    totpCreate: boolean;
    totpEnroll: boolean;
    totpUnenroll: boolean;
    smsCreate: boolean;
    smsEnroll: boolean;
    smsUnenroll: boolean;
  };
  setLoadingStates: React.Dispatch<
    React.SetStateAction<{
      totpCreate: boolean;
      totpEnroll: boolean;
      totpUnenroll: boolean;
      smsCreate: boolean;
      smsEnroll: boolean;
      smsUnenroll: boolean;
    }>
  >;
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

const ProfileContext = createContext<ProfileContextType | undefined>(undefined);

interface ProfileProviderProps {
  auth: useAuthType;
  children: ReactNode;
  providers: string[];
}

export const ProfileProvider = ({
  children,
  providers,
  auth,
}: ProfileProviderProps) => {
  const router = useRouter();

  const [loadingStates, setLoadingStates] = useState({
    totpCreate: false,
    totpEnroll: false,
    totpUnenroll: false,
    smsCreate: false,
    smsEnroll: false,
    smsUnenroll: false,
  });

  const [deactivateModal, setDeactivateModal] = useState<{
    isOpen: boolean;
    type: 'totp' | 'sms' | null;
  }>({
    isOpen: false,
    type: null,
  });

  const value: ProfileContextType = {
    auth,
    router,
    providers,
    loadingStates,
    setLoadingStates,
    deactivateModal,
    setDeactivateModal,
  };

  return (
    <ProfileContext.Provider value={value}>{children}</ProfileContext.Provider>
  );
};

export const useProfileContext = () => {
  const context = useContext(ProfileContext);
  if (context === undefined) {
    throw new Error("useProfileContext must be used within a ProfileProvider");
  }
  return context;
};