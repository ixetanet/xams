import {
  AuthConfig,
  AuthConfigOptions,
  Factor,
  getMobileDevice,
  AuthResponse,
} from "@ixeta/headless-auth-react";
import { SupabaseClient, Provider } from "@supabase/supabase-js";

export type SupabaseAuthConfigOptions = {
  totpAppName?: string;
};

export class SupabaseAuthConfig implements AuthConfig {
  private authStateCallback: ((isLoggedIn: boolean) => void) | null = null;
  private isMfaRequired: boolean = false;
  private supabaseError: any = null;
  private totpFactorId: string | null = null;
  private smsFactorId: string | null = null;
  private smsPhoneNumber: string | null = null;
  private unsubscribe: (() => void) | null = null;

  constructor(
    public supabaseClient: SupabaseClient,
    public options?: AuthConfigOptions & SupabaseAuthConfigOptions
  ) {}

  private reset = () => {
    this.isMfaRequired = false;
    this.supabaseError = null;
    this.totpFactorId = null;
    this.smsFactorId = null;
    this.smsPhoneNumber = null;
  };

  setOptions = (options: AuthConfigOptions & SupabaseAuthConfigOptions) => {
    this.options = options;
  };

  getAccessToken = async () => {
    const { data } = await this.supabaseClient.auth.getSession();
    return data.session?.access_token;
  };

  signUp = async (
    emailAddress: string,
    password: string,
    remember: boolean
  ) => {
    try {
      const { data, error } = await this.supabaseClient.auth.signUp({
        email: emailAddress,
        password: password,
      });

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      if (data.user && this.options?.onSignUpSuccess) {
        await this.options.onSignUpSuccess(this);
      }

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  signIn = async (
    emailAddress: string,
    password: string,
    remember: boolean
  ) => {
    try {
      this.reset();
      const { data, error } = await this.supabaseClient.auth.signInWithPassword(
        {
          email: emailAddress,
          password: password,
        }
      );

      if (error) {
        // Check if MFA is required
        if (error.message.includes("mfa")) {
          this.isMfaRequired = true;
          return {
            success: false,
            error: "Multi-factor authentication is required.",
          };
        }

        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      if (this.options?.onSignInSuccess) {
        await this.options.onSignInSuccess(this);
      }

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  signOut = async () => {
    try {
      this.reset();
      const { error } = await this.supabaseClient.auth.signOut();

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      if (this.options?.onSignOutSuccess) {
        await this.options.onSignOutSuccess(this);
      }

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  private getProvider = (providerName: string): Provider => {
    switch (providerName.toLowerCase()) {
      case "google":
        return "google";
      case "facebook":
        return "facebook";
      case "twitter":
        return "twitter";
      case "github":
        return "github";
      case "apple":
        return "apple";
      case "microsoft":
        return "azure";
      case "slack":
        return "slack";
      case "discord":
        return "discord";
      case "gitlab":
        return "gitlab";
      case "bitbucket":
        return "bitbucket";
      default:
        throw new Error(`Unsupported provider: ${providerName}`);
    }
  };

  private providerSignIn = async (provider: Provider) => {
    try {
      const isMobile = getMobileDevice() !== "unknown";

      // For mobile or localhost, use popup
      if (isMobile || window.location.hostname === "localhost") {
        const { data, error } = await this.supabaseClient.auth.signInWithOAuth({
          provider: provider,
          options: {
            redirectTo: window.location.origin,
          },
        });

        if (error) {
          return {
            success: false,
            error: this.friendlyError(error),
          };
        }

        return {
          success: true,
        };
      } else {
        // For desktop, use redirect
        const { data, error } = await this.supabaseClient.auth.signInWithOAuth({
          provider: provider,
          options: {
            redirectTo: window.location.origin,
          },
        });

        if (error) {
          return {
            success: false,
            error: this.friendlyError(error),
          };
        }

        return {
          success: true,
        };
      }
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  signInProvider = async (providerName: string) => {
    this.reset();
    try {
      const provider = this.getProvider(providerName);
      return await this.providerSignIn(provider);
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  handleRedirectResult = async () => {
    try {
      const { data, error } = await this.supabaseClient.auth.getSession();

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      if (data.session) {
        if (this.options?.onSignInSuccess) {
          await this.options.onSignInSuccess(this);
        }
        return {
          success: true,
        };
      }

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  onAuthStateChanged = (callback: (isLoggedIn: boolean) => void) => {
    this.authStateCallback = callback;

    // Initial check
    this.supabaseClient.auth.getSession().then(({ data }) => {
      const isLoggedIn = !!data.session;
      callback(isLoggedIn);
    });

    // Subscribe to auth changes
    const { data } = this.supabaseClient.auth.onAuthStateChange(
      (event, session) => {
        const isLoggedIn = !!session;

        if (event === "SIGNED_IN") {
          console.log("User signed in");
          callback(true);
          if (this.options?.onSignInSuccess) {
            this.options.onSignInSuccess(this);
          }
        } else if (event === "SIGNED_OUT") {
          console.log("User signed out");
          callback(false);
        } else if (event === "USER_UPDATED") {
          console.log("User updated");
          callback(isLoggedIn);
        }
      }
    );

    this.unsubscribe = data.subscription.unsubscribe;

    return () => {
      if (this.unsubscribe) {
        this.unsubscribe();
      }
    };
  };

  isEmailVerified = async () => {
    const { data } = await this.supabaseClient.auth.getUser();
    return data.user?.email_confirmed_at != null;
  };

  mfaRequired = async () => {
    return this.isMfaRequired;
  };

  /** User is not logged in, and prompted for 2nd factor upon attempting to log in */
  mfaFactors = async () => {
    try {
      const { data, error } =
        await this.supabaseClient.auth.mfa.getAuthenticatorAssuranceLevel();

      if (error) {
        console.error("Error getting MFA factors:", error);
        return [];
      }

      const factors: Factor[] = [];

      // Check enrolled factors
      const { data: enrolledFactors, error: factorsError } =
        await this.supabaseClient.auth.mfa.listFactors();

      if (factorsError) {
        console.error("Error listing MFA factors:", factorsError);
        return [];
      }

      for (const factor of enrolledFactors.totp) {
        factors.push("totp");
        this.totpFactorId = factor.id;
      }

      return factors;
    } catch (error) {
      console.error("Error checking MFA factors:", error);
      return [];
    }
  };

  /** User is logged in, return true if TOTP enrolled */
  mfaTotpEnrolled = async () => {
    try {
      const { data, error } = await this.supabaseClient.auth.mfa.listFactors();

      if (error) {
        console.error("Error checking TOTP enrollment:", error);
        return false;
      }

      const hasTotpFactor = data.totp.length > 0;

      if (hasTotpFactor && data.totp[0].id) {
        this.totpFactorId = data.totp[0].id;
      }

      return hasTotpFactor;
    } catch (error) {
      console.error("Error checking TOTP enrollment:", error);
      return false;
    }
  };

  mfaTotpCreate = async () => {
    try {
      const { data, error } = await this.supabaseClient.auth.mfa.enroll({
        factorType: "totp",
      });

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
          data: error.message.includes("recent login") ? "re-login" : undefined,
        };
      }

      if (!data.totp) {
        return {
          success: false,
          error: "Failed to create TOTP factor",
        };
      }

      this.totpFactorId = data.id;

      // Return the QR code URI
      return {
        success: true,
        data: data.totp.uri,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  mfaTotpEnroll = async (code: string) => {
    try {
      if (!this.totpFactorId) {
        return {
          success: false,
          error: "No TOTP factor available. Please generate a new one.",
        };
      }

      const { data, error } = await this.supabaseClient.auth.mfa.challenge({
        factorId: this.totpFactorId,
      });

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      const { data: verifyData, error: verifyError } =
        await this.supabaseClient.auth.mfa.verify({
          factorId: this.totpFactorId,
          challengeId: data.id,
          code,
        });

      if (verifyError) {
        return {
          success: false,
          error: this.friendlyError(verifyError),
        };
      }

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  mfaTotpUnenroll = async () => {
    try {
      if (!this.totpFactorId) {
        const enrolled = await this.mfaTotpEnrolled();
        if (!enrolled || !this.totpFactorId) {
          return {
            success: false,
            error: "No TOTP factor found.",
          };
        }
      }

      const { error } = await this.supabaseClient.auth.mfa.unenroll({
        factorId: this.totpFactorId!,
      });

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      this.totpFactorId = null;

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  mfaTotpVerify = async (code: string) => {
    try {
      if (!this.totpFactorId) {
        return {
          success: false,
          error: "No TOTP factor found.",
        };
      }

      const { data, error } = await this.supabaseClient.auth.mfa.challenge({
        factorId: this.totpFactorId,
      });

      if (error) {
        return {
          success: false,
          error: this.friendlyError(error),
        };
      }

      const { data: verifyData, error: verifyError } =
        await this.supabaseClient.auth.mfa.verify({
          factorId: this.totpFactorId,
          challengeId: data.id,
          code,
        });

      if (verifyError) {
        return {
          success: false,
          error: this.friendlyError(verifyError),
        };
      }

      this.isMfaRequired = false;

      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  /** User is logged in, return true if SMS enrolled */
  mfaSmsEnrolled = async () => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    return false;
  };

  mfaSmsCreate = async (phoneNumber: string) => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    this.smsPhoneNumber = phoneNumber;

    return {
      success: false,
      error: "SMS MFA is not currently supported with Supabase.",
    };
  };

  mfaSmsEnroll = async (code: string) => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    return {
      success: false,
      error: "SMS MFA is not currently supported with Supabase.",
    };
  };

  mfaSmsUnenroll = async () => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    return {
      success: false,
      error: "SMS MFA is not currently supported with Supabase.",
    };
  };

  mfaSmsSend = async () => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    return {
      success: false,
      error: "SMS MFA is not currently supported with Supabase.",
    };
  };

  mfaSmsVerify = async (code: string) => {
    // Supabase currently doesn't support SMS MFA directly
    // This is a placeholder for future implementation
    return {
      success: false,
      error: "SMS MFA is not currently supported with Supabase.",
    };
  };

  private friendlyError = (error: any): string => {
    this.supabaseError = error;

    if (!error) {
      return "An unknown error occurred";
    }

    if (error.message && error.message.includes("mfa")) {
      this.isMfaRequired = true;
      return "Multi-factor authentication is required.";
    }

    // Map Supabase error messages to user-friendly messages
    const message = error.message || "";

    if (message.includes("Email not confirmed")) {
      return "Your email is not verified.";
    }

    if (message.includes("Invalid login credentials")) {
      return "Invalid email or password.";
    }

    if (message.includes("Email already registered")) {
      return "This email is already in use by another account.";
    }

    if (message.includes("Password should be at least")) {
      return "Your password is too weak.";
    }

    if (message.includes("Invalid email")) {
      return "The email address is not valid.";
    }

    if (message.includes("No user found")) {
      return "No user found with this email.";
    }

    if (message.includes("User already registered")) {
      return "This user is already registered.";
    }

    if (message.includes("Rate limit")) {
      return "Too many requests. Please try again later.";
    }

    if (message.includes("network")) {
      return "Network error. Please check your connection.";
    }

    if (message.includes("recent login")) {
      return "Please log in again to complete this action.";
    }

    // Return the original message if no specific mapping is found
    return message || "An unexpected error occurred. Please try again.";
  };
}
