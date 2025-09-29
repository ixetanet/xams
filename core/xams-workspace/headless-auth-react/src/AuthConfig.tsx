import { AuthConfigOptions } from "./AuthConfigOptions";

export type Factor = "totp" | "sms" | "email";

export type AuthConfig = {
  setOptions: (options: AuthConfigOptions) => void;
  signUp: (
    emailAddress: string,
    password: string,
    remember: boolean
  ) => Promise<AuthResponse>;
  signIn: (
    emailAddress: string,
    password: string,
    remember: boolean
  ) => Promise<AuthResponse>;
  signInProvider: (providerName: string) => Promise<AuthResponse>;
  signOut: () => Promise<AuthResponse>;
  getAccessToken: () => Promise<string | undefined>;
  handleRedirectResult: () => Promise<AuthResponse>;
  onAuthStateChanged: (callback: (isLoggedIn: boolean) => void) => () => void;
  isEmailVerified: () => Promise<boolean>;
  mfaRequired: () => Promise<boolean>;
  mfaFactors: () => Promise<Factor[]>;
  mfaTotpEnrolled: () => Promise<boolean>;
  mfaTotpCreate: () => Promise<AuthResponse<string>>;
  mfaTotpEnroll: (code: string) => Promise<AuthResponse>;
  mfaTotpUnenroll: () => Promise<AuthResponse>;
  mfaTotpVerify: (code: string) => Promise<AuthResponse>;
  mfaSmsEnrolled: () => Promise<boolean>;
  mfaSmsCreate: (phoneNumber: string) => Promise<AuthResponse<string>>;
  mfaSmsEnroll: (code: string) => Promise<AuthResponse>;
  mfaSmsUnenroll: () => Promise<AuthResponse>;
  mfaSmsSend: () => Promise<AuthResponse>;
  mfaSmsVerify: (code: string) => Promise<AuthResponse>;
  changePassword: (
    currentPassword: string,
    newPassword: string
  ) => Promise<AuthResponse>;
  sendEmailVerification: (settings?: any) => Promise<AuthResponse>;
  sendPasswordResetEmail: (emailAddress: string) => Promise<AuthResponse>;
  verifyPasswordResetCode: (oobCode: string) => Promise<AuthResponse<string>>;
  confirmPasswordReset: (
    oobCode: string,
    newPassword: string
  ) => Promise<AuthResponse>;
};

export type AuthResponse<T = any> = {
  success: boolean;
  error?: string;
  data?: T;
};
