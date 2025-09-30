import React, {
  createContext,
  useContext,
  ReactNode,
  useEffect,
  useRef,
  useState,
} from "react";
import { AuthConfig, Factor } from "./AuthConfig";
import { AuthStateChangeContext } from "./useAuth";

export type AuthContextType = {
  authConfig: AuthConfig;
  setErrorRef: React.MutableRefObject<
    React.Dispatch<React.SetStateAction<string | null>>
  >;
  clearRef: React.MutableRefObject<() => void>;
  onAuthStateChangedRef: React.MutableRefObject<
    (context: AuthStateChangeContext) => void
  >;
  isReady: boolean;
  isLoggedIn: boolean;
  isMfaRequired: boolean;
  mfaFactors: Factor[];
  isEmailVerified: boolean;
  hasPasswordProvider: boolean;
  mfaTotpEnrolled: boolean;
  mfaSmsEnrolled: boolean;
  setMfaTotpEnrolled: React.Dispatch<React.SetStateAction<boolean>>;
  setMfaSmsEnrolled: React.Dispatch<React.SetStateAction<boolean>>;
  setIsMfaRequired: React.Dispatch<React.SetStateAction<boolean>>;
};

export const AuthContext = createContext<AuthContextType | undefined>(
  undefined
);

type AuthProviderProps = {
  authConfig: AuthConfig | null;
  children: ReactNode;
};

export const AuthProvider = ({ authConfig, children }: AuthProviderProps) => {
  const [isLoggedIn, setIsLoggedIn] = useState<boolean | undefined>(undefined);
  const [isEmailVerified, setIsEmailVerified] = useState<boolean>(false);
  const [hasPasswordProvider, setHasPasswordProvider] =
    useState<boolean>(false);
  const [isMfaRequired, setIsMfaRequired] = useState<boolean>(false);
  const [mfaFactors, setMfaFactors] = useState<Factor[]>([]);
  const [mfaTotpEnrolled, setMfaTotpEnrolled] = useState<boolean>(false);
  const [mfaSmsEnrolled, setMfaSmsEnrolled] = useState<boolean>(false);
  const [, setError] = useState<string | null>(null);
  const setErrorRef = useRef(setError);
  const clearRef = useRef(() => {});
  const onAuthStateChangedRef = useRef((_: AuthStateChangeContext) => {});

  const handleRedirectResult = async () => {
    if (!authConfig) return;
    if (authConfig.handleRedirectResult) {
      const resp = await authConfig.handleRedirectResult();
      if (!resp.success) {
        setErrorRef.current(resp.error ?? "Unknown error");
      }
    }
  };

  const handleAuthStateChanged = async (isLoggedIn: boolean) => {
    if (!authConfig) return;
    clearRef.current();
    const isMfaRequired = await authConfig.mfaRequired();
    let mfaFactors: Factor[] = [];
    if (isMfaRequired) {
      mfaFactors = await authConfig.mfaFactors();
      setMfaFactors(mfaFactors);
    }

    if (isLoggedIn) {
      const isEmailVerified = await authConfig.isEmailVerified();
      const hasPasswordProvider = await authConfig.hasPasswordProvider();
      const hasMfaTotp = await authConfig.mfaTotpEnrolled();
      const hasSmsTotp = await authConfig.mfaSmsEnrolled();
      setIsEmailVerified(isEmailVerified);
      setHasPasswordProvider(hasPasswordProvider);
      setIsLoggedIn(isLoggedIn);
      setMfaTotpEnrolled(hasMfaTotp);
      setMfaSmsEnrolled(hasSmsTotp);
      setIsMfaRequired(isMfaRequired);
      onAuthStateChangedRef.current({
        isLoggedIn: isLoggedIn,
        isEmailVerified: isEmailVerified,
        hasPasswordProvider: hasPasswordProvider,
        mfaTotpEnrolled: hasMfaTotp,
        mfaSmsEnrolled: hasSmsTotp,
        isMfaRequired: isMfaRequired,
        mfaFactors: mfaFactors,
      });
    } else {
      setIsLoggedIn(isLoggedIn);
      setIsEmailVerified(false);
      setHasPasswordProvider(false);
      setMfaTotpEnrolled(false);
      setMfaSmsEnrolled(false);
      setIsMfaRequired(isMfaRequired);
      setMfaFactors(mfaFactors);
      onAuthStateChangedRef.current({
        isLoggedIn: isLoggedIn,
        isEmailVerified: false,
        hasPasswordProvider: false,
        mfaTotpEnrolled: false,
        mfaSmsEnrolled: false,
        isMfaRequired: isMfaRequired,
        mfaFactors: mfaFactors,
      });
    }
  };

  useEffect(() => {
    if (!authConfig) return;
    handleRedirectResult();
    const unsubscribe = authConfig.onAuthStateChanged((isLoggedIn) => {
      handleAuthStateChanged(isLoggedIn);
    });
    return () => {
      unsubscribe();
    };
  }, [authConfig, setIsLoggedIn]);

  return (
    <AuthContext.Provider
      value={{
        authConfig: authConfig ?? ({} as AuthConfig),
        setErrorRef: setErrorRef,
        clearRef: clearRef,
        onAuthStateChangedRef: onAuthStateChangedRef,
        isReady: isLoggedIn !== undefined,
        isLoggedIn: isLoggedIn === true,
        isMfaRequired: isMfaRequired,
        mfaFactors: mfaFactors,
        isEmailVerified: isEmailVerified,
        hasPasswordProvider: hasPasswordProvider,
        mfaTotpEnrolled: mfaTotpEnrolled,
        mfaSmsEnrolled: mfaSmsEnrolled,
        setMfaTotpEnrolled: setMfaTotpEnrolled,
        setMfaSmsEnrolled: setMfaSmsEnrolled,
        setIsMfaRequired: setIsMfaRequired,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuthContext = () => {
  const authContext = useContext(AuthContext);

  if (authContext === undefined) {
    throw new Error("useAuth must be used within a AuthProvider");
  }
  return authContext;
};
