import { useContext, useState } from "react";
import { AuthContext, AuthContextType } from "./AuthContext";
import { AuthResponse, Factor } from "./AuthConfig";

export type AuthStateChangeContext = {
  isLoggedIn: boolean;
  isMfaRequired: boolean;
  isEmailVerified: boolean;
  mfaTotpEnrolled: boolean;
  mfaSmsEnrolled: boolean;
  mfaFactors: Factor[];
};

interface useAuthProps {
  defaultView?: string;
  onAuthStateChanged?: (context: AuthStateChangeContext) => void;
}

export const useAuth = (props?: useAuthProps) => {
  const authContext = useContext(AuthContext);
  const [view, setView] = useState<string>(props?.defaultView ?? "login");
  const [error, setError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [emailAddress, setEmailAddress] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(true);
  const [mfaTotpQrCode, setMfaTotpQrCode] = useState<undefined | string>(
    undefined
  );
  const [mfaCode, setMfaCode] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [isReLoginRequired, setIsReLoginRequired] = useState(false);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [resetEmail, setResetEmail] = useState("");

  if (authContext === undefined) {
    throw new Error("useAuth must be used within a AuthProvider");
  }

  const clear = () => {
    setError(null);
    setEmailAddress("");
    setPassword("");
    setRemember(true);
    setIsReLoginRequired(false);
    setMfaTotpQrCode(undefined);
    setMfaCode("");
    setPhoneNumber("");
    setCurrentPassword("");
    setNewPassword("");
    setResetEmail("");
  };

  const onSetView = (view: string) => {
    setView(view);
    clear();
  };

  authContext.setErrorRef.current = setError;
  authContext.clearRef.current = clear;
  if (props?.onAuthStateChanged != null) {
    authContext.onAuthStateChangedRef.current = props.onAuthStateChanged;
  }

  const execute = async (callback: () => Promise<AuthResponse>) => {
    try {
      setIsProcessing(true);
      const resp = await callback();
      if (!resp.success) {
        setError(resp.error ?? "Unknown error");
      } else {
        setError(null);
      }
      return resp.success;
    } catch (error) {
      console.error("Error processing request: ", error);
      return false;
    } finally {
      setIsProcessing(false);
    }
  };

  const signIn = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.signIn(
        emailAddress,
        password,
        remember
      );
      if (resp.success) {
        setIsReLoginRequired(false);
      }
      return resp;
    });
  };
  const signInProvider = async (providerName: string) => {
    return await execute(async () => {
      const resp = await authContext.authConfig.signInProvider(providerName);
      if (resp.success) {
        setIsReLoginRequired(false);
      }
      return resp;
    });
  };
  const signUp = async () => {
    return await execute(async () => {
      return await authContext.authConfig.signUp(
        emailAddress,
        password,
        remember
      );
    });
  };
  const signOut = async (view?: string) => {
    return await execute(async () => {
      const resp = await authContext.authConfig.signOut();
      authContext.setIsMfaRequired(false);
      if (view) {
        setView(view);
      } else {
        setView(props?.defaultView ?? "login");
      }
      return resp;
    });
  };

  const mfaTotpCreate = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaTotpCreate();
      if (!resp.success && resp.data === "re-login") {
        setIsReLoginRequired(true);
        setView(props?.defaultView ?? "login");
      }
      if (resp.success) {
        setMfaTotpQrCode(resp.data ?? "");
      }
      return resp;
    });
  };

  const mfaTotpEnroll = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaTotpEnroll(mfaCode);
      if (resp.success) {
        setMfaTotpQrCode(undefined);
        const enrolled = await authContext.authConfig.mfaTotpEnrolled();
        if (enrolled) {
          authContext.setMfaTotpEnrolled(true);
        }
      }
      return resp;
    });
  };

  const mfaTotpUnenroll = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaTotpUnenroll();
      if (!resp.success && resp.data === "re-login") {
        setIsReLoginRequired(true);
        setView("login");
      }
      if (resp.success) {
        authContext.setMfaTotpEnrolled(false);
      }
      return resp;
    });
  };

  const mfaTotpVerify = async () => {
    return await execute(async () => {
      return await authContext.authConfig.mfaTotpVerify(mfaCode);
    });
  };

  const mfaSmsCreate = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaSmsCreate(phoneNumber);
      // Could be incorrect phone number format OR require re-login
      if (!resp.success && resp.data === "re-login") {
        setIsReLoginRequired(true);
        setView("login");
      }
      return resp;
    });
  };

  const mfaSmsEnroll = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaSmsEnroll(mfaCode);
      if (resp.success) {
        authContext.setMfaSmsEnrolled(true);
      }
      return resp;
    });
  };

  const mfaSmsUnenroll = async () => {
    return await execute(async () => {
      const resp = await authContext.authConfig.mfaSmsUnenroll();
      if (!resp.success && resp.data === "re-login") {
        setIsReLoginRequired(true);
        setView("login");
      }
      if (resp.success) {
        authContext.setMfaSmsEnrolled(false);
      }
      return resp;
    });
  };

  const mfaSmsSend = async () => {
    return await execute(async () => {
      return await authContext.authConfig.mfaSmsSend();
    });
  };
  const mfaSmsVerify = async () => {
    return await execute(async () => {
      return await authContext.authConfig.mfaSmsVerify(mfaCode);
    });
  };

  const changePassword = async (
    currentPwd?: string,
    newPwd?: string
  ) => {
    return await execute(async () => {
      const resp = await authContext.authConfig.changePassword(
        currentPwd || currentPassword,
        newPwd || newPassword
      );
      if (resp.success) {
        setCurrentPassword("");
        setNewPassword("");
      }
      return resp;
    });
  };

  const sendEmailVerification = async () => {
    return await execute(async () => {
      return await authContext.authConfig.sendEmailVerification();
    });
  };

  const sendPasswordResetEmail = async () => {
    return await execute(async () => {
      return await authContext.authConfig.sendPasswordResetEmail(resetEmail);
    });
  };

  return {
    view,
    setView: onSetView,
    isProcessing,
    emailAddress,
    setEmailAddress,
    password,
    setPassword,
    remember,
    setRemember,
    mfaTotpQrCode: mfaTotpQrCode,
    isReLoginRequired,
    mfaCode,
    setMfaCode,
    phoneNumber,
    setPhoneNumber,
    signIn,
    signInProvider,
    signUp,
    signOut,
    error,
    mfaTotpCreate: mfaTotpCreate,
    mfaTotpEnroll,
    mfaTotpUnenroll,
    mfaTotpVerify,
    mfaSmsCreate: mfaSmsCreate,
    mfaSmsEnroll,
    mfaSmsUnenroll,
    mfaSmsSend,
    mfaSmsVerify,
    currentPassword,
    setCurrentPassword,
    newPassword,
    setNewPassword,
    changePassword,
    sendEmailVerification,
    resetEmail,
    setResetEmail,
    sendPasswordResetEmail,
    ...authContext,
  };
};

export type useAuthType = ReturnType<typeof useAuth>;
