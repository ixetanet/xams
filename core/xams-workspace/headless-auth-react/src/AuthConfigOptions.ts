import { AuthConfig } from "./AuthConfig";

export type AuthConfigOptions = {
  onSignUpSuccess?: (authConfig: AuthConfig) => Promise<void>;
  onSignInSuccess?: (authConfig: AuthConfig) => Promise<void>;
  onSignOutSuccess?: (authConfig: AuthConfig) => Promise<void>;
  onSignInError?: (error: Error) => Promise<void>;
};
