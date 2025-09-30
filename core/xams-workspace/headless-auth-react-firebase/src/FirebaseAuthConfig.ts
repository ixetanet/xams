import {
  AuthConfig,
  AuthConfigOptions,
  Factor,
  getMobileDevice,
} from "@ixeta/headless-auth-react";
import { AuthResponse } from "@ixeta/headless-auth-react/src/AuthConfig";
import {
  FacebookAuthProvider,
  GoogleAuthProvider,
  browserLocalPersistence,
  browserSessionPersistence,
  createUserWithEmailAndPassword,
  getRedirectResult,
  signInWithEmailAndPassword,
  signInWithPopup,
  signInWithRedirect,
  Auth,
  AuthProvider,
  TwitterAuthProvider,
  GithubAuthProvider,
  OAuthProvider,
  multiFactor,
  TotpMultiFactorGenerator,
  TotpSecret,
  getMultiFactorResolver,
  MultiFactorResolver,
  RecaptchaVerifier,
  PhoneAuthProvider,
  PhoneMultiFactorGenerator,
  updatePassword,
  reauthenticateWithCredential,
  EmailAuthProvider,
  sendEmailVerification,
  sendPasswordResetEmail,
  verifyPasswordResetCode,
  confirmPasswordReset,
  ActionCodeSettings,
} from "firebase/auth";

export type FirebaseAuthConfigOptions = {
  totpAppName?: string;
};

export class FirebaseAuthConfig implements AuthConfig {
  private authStateCallback: ((isLoggedIn: boolean) => void) | null = null;
  private totpSecret: TotpSecret | null = null;
  private isMfaRequired: boolean = false;
  private firebaseError: any = null;
  private mfaResolver: MultiFactorResolver | null = null;
  private smsVerificationId: string | null = null;
  private smsPhoneNumber: string | null = null;
  private recaptchaVerifier: RecaptchaVerifier | null = null;

  constructor(
    public auth: Auth,
    public options?: AuthConfigOptions & FirebaseAuthConfigOptions
  ) {}

  private reset = (resetRecaptcha?: boolean) => {
    this.totpSecret = null;
    this.isMfaRequired = false;
    this.firebaseError = null;
    this.mfaResolver = null;
    this.smsVerificationId = null;
    this.smsPhoneNumber = null;
    if (resetRecaptcha) {
      this.recaptchaVerifier?.clear();
      this.recaptchaVerifier = null;
    }
  };
  private setPersistence = async (remember: boolean) => {
    if (remember) {
      await this.auth.setPersistence(browserLocalPersistence);
    } else {
      await this.auth.setPersistence(browserSessionPersistence);
    }
  };

  setOptions = (options: AuthConfigOptions & FirebaseAuthConfigOptions) => {
    this.options = options;
  };

  getAccessToken = async () => {
    const token = await this.auth.currentUser?.getIdToken();
    return token;
  };

  signUp = async (
    emailAddress: string,
    password: string,
    remember: boolean
  ) => {
    try {
      await this.setPersistence(remember);
      const userCredential = await createUserWithEmailAndPassword(
        this.auth,
        emailAddress,
        password
      );
      if (userCredential.user != null && this.options?.onSignUpSuccess) {
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
      this.mfaRequired;
      await this.setPersistence(remember);
      const userCredential = await signInWithEmailAndPassword(
        this.auth,
        emailAddress,
        password
      );
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
      this.reset(true);
      await this.auth.signOut();
      if (this.options?.onSignOutSuccess) {
        this.options.onSignOutSuccess(this);
      }
      return {
        success: true,
      };
    } catch (error) {
      console.error(error);
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  private providerSignIn = async (provider: AuthProvider) => {
    this.auth.useDeviceLanguage();
    /* Cannot use redirect on localhost using a custom authDomain, the firebase helper scripts and the redirect domain need to be the same
    see: https://stackoverflow.com/questions/77572387/using-signinwithredirect-with-an-authdomain-different-than-the-domain-of-the
    Redirect is also not working on iPhone (popup works on iOS and Android, but this is working on desktop)
    Redirect Best Practices: https://firebase.google.com/docs/auth/web/redirect-best-practices
    */
    try {
      if (
        window.location.hostname === "localhost" ||
        getMobileDevice() !== "unknown" // iOS and Android
      ) {
        const userCred = await signInWithPopup(this.auth, provider);
      } else {
        await signInWithRedirect(this.auth, provider);
      }
      return {
        success: true,
      };
    } catch (error) {
      console.error(error);
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  private getProvider = (providerName: string) => {
    let provider: AuthProvider;
    switch (providerName) {
      case "google":
        provider = new GoogleAuthProvider();
        break;
      case "facebook":
        provider = new FacebookAuthProvider();
        break;
      case "twitter":
        provider = new TwitterAuthProvider();
        break;
      case "github":
        provider = new GithubAuthProvider();
        break;
      case "apple":
        provider = new OAuthProvider("apple.com");
        break;
      case "microsoft":
        provider = new OAuthProvider("microsoft.com");
        break;
      case "yahoo":
        provider = new OAuthProvider("yahoo.com");
        break;
      default:
        throw new Error(`Unsupported provider: ${providerName}`);
    }
    return provider;
  };

  signInProvider = async (providerName: string) => {
    this.reset();
    const provider = this.getProvider(providerName);
    return await this.providerSignIn(provider);
  };

  handleRedirectResult = async () => {
    try {
      const result = await getRedirectResult(this.auth);
      if (result) {
        // The result object contains the provider ID
        console.log("Redirect result from provider:", result.providerId);

        // You can determine which provider was used
        let credential;

        switch (result.providerId) {
          case GoogleAuthProvider.PROVIDER_ID:
            credential = GoogleAuthProvider.credentialFromResult(result);
            break;
          case FacebookAuthProvider.PROVIDER_ID:
            credential = FacebookAuthProvider.credentialFromResult(result);
            break;
          case TwitterAuthProvider.PROVIDER_ID:
            credential = TwitterAuthProvider.credentialFromResult(result);
            break;
          case GithubAuthProvider.PROVIDER_ID:
            credential = GithubAuthProvider.credentialFromResult(result);
            break;
          case "apple.com":
            credential = OAuthProvider.credentialFromResult(result);
            break;
          case "microsoft.com":
            credential = OAuthProvider.credentialFromResult(result);
            break;
          case "yahoo.com":
            credential = OAuthProvider.credentialFromResult(result);
            break;
          default:
            // Handle other providers if needed
            console.log("Other provider:", result.providerId);
        }
        const user = result.user;
        return {
          success: true,
        };
      }
      return {
        success: true,
      };
    } catch (error) {
      console.error("Redirect result error:", error);
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  onAuthStateChanged = (callback: (isLoggedIn: boolean) => void) => {
    this.authStateCallback = callback;
    const unsubscribe = this.auth.onAuthStateChanged(async (user) => {
      if (user) {
        console.log("User is signed in.");
        if (this.options?.onSignInSuccess) {
          await this.options.onSignInSuccess(this);
        }
        callback(true);
      } else {
        console.log("No user is signed in.");
        callback(false);
      }
    });
    return () => unsubscribe();
  };

  isEmailVerified = async () => {
    await this.auth.authStateReady();
    const user = this.auth.currentUser;

    if (!user) {
      return false;
    }

    // Always refresh user properties from backend
    await user.reload();

    // Check if user signed in with any OAuth providers (not password/phone)
    const hasOAuthProvider = user.providerData.some(
      (provider) =>
        provider.providerId !== "password" && provider.providerId !== "phone"
    );

    // OAuth providers have already verified the email
    if (hasOAuthProvider) {
      return true;
    }

    return user.emailVerified;
  };

  hasPasswordProvider = async () => {
    await this.auth.authStateReady();
    const user = this.auth.currentUser;

    if (!user) {
      return false;
    }

    // Check if user has password provider
    return user.providerData.some(
      (provider) => provider.providerId === "password"
    );
  };

  mfaRequired = async () => {
    return this.isMfaRequired;
  };

  /** User is not logged in, and prompted for 2nd factor upon attempting to log in */
  mfaFactors = async () => {
    if (this.mfaResolver == null) {
      return [];
    }
    const enrolledFactors = this.mfaResolver.hints.map((info) => info.factorId);
    const results = [] as Factor[];
    if (enrolledFactors.includes("totp")) {
      results.push("totp");
    }
    if (enrolledFactors.includes("phone")) {
      results.push("sms");
    }
    return results;
  };

  /** User is logged in, return true if TOTP enrolled */
  mfaTotpEnrolled = async () => {
    const user = this.auth.currentUser;
    if (!user) {
      return false;
    }

    try {
      const multiFactorUser = multiFactor(user);
      const enrolledFactors = multiFactorUser.enrolledFactors;
      const hasTotpFactor = enrolledFactors.some(
        (factor) => factor.factorId === "totp"
      );
      return hasTotpFactor;
    } catch (error) {
      console.error("Error checking TOTP enrollment:", error);
      return false;
    }
  };

  mfaTotpCreate = async () => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }
      const multiFactorSession = await multiFactor(currentUser).getSession();
      this.totpSecret = await TotpMultiFactorGenerator.generateSecret(
        multiFactorSession
      );
      const totpUri = this.totpSecret.generateQrCodeUrl(
        currentUser.email as string,
        this.options?.totpAppName ?? "Firebase"
      );
      return {
        success: true,
        data: totpUri,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
        data:
          (error as any)?.code === "auth/requires-recent-login"
            ? "re-login"
            : undefined,
      };
    }
  };

  mfaTotpEnroll = async (code: string) => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }
      if (this.totpSecret == null) {
        return {
          success: false,
          error: "TOTP secret is not available. Please generate a new one.",
        };
      }
      const multiFactorAssertion =
        TotpMultiFactorGenerator.assertionForEnrollment(this.totpSecret, code);
      await multiFactor(currentUser).enroll(
        multiFactorAssertion,
        this.options?.totpAppName ?? "Firebase"
      );
      return {
        success: true,
      };
    } catch (error) {
      console.error("Error enrolling TOTP:", error);
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  mfaTotpUnenroll = async () => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }

      const multiFactorUser = multiFactor(currentUser);
      const enrolledFactors = multiFactorUser.enrolledFactors;
      const totpFactor = enrolledFactors.find(
        (factor) => factor.factorId === "totp"
      );

      if (!totpFactor) {
        return {
          success: false,
          error: "No TOTP factor found.",
        };
      }

      await multiFactorUser.unenroll(totpFactor);
      return {
        success: true,
      };
    } catch (error) {
      console.error("Error unenrolling TOTP:", error);
      return {
        success: false,
        error: this.friendlyError(error),
        data:
          (error as any)?.code === "auth/requires-recent-login"
            ? "re-login"
            : undefined,
      };
    }
  };

  mfaTotpVerify = async (code: string) => {
    try {
      if (!this.mfaResolver) {
        return {
          success: false,
          error: "No multi-factor resolver available.",
        };
      }
      const uid = this.mfaResolver.hints.find(
        (info) => info.factorId === "totp"
      )?.uid;
      if (!uid) {
        return {
          success: false,
          error: "No TOTP factor found.",
        };
      }
      const multiFactorAssertion = TotpMultiFactorGenerator.assertionForSignIn(
        uid,
        code
      );
      const userCredential = await this.mfaResolver.resolveSignIn(
        multiFactorAssertion
      );
      this.isMfaRequired = false;
      if (this.authStateCallback) {
        this.authStateCallback(this.auth.currentUser != null);
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

  /** User is logged in, return true if SMS enrolled */
  mfaSmsEnrolled = async () => {
    const user = this.auth.currentUser;
    if (!user) {
      return false;
    }

    try {
      const multiFactorUser = multiFactor(user);
      const enrolledFactors = multiFactorUser.enrolledFactors;
      const hasSmsFactor = enrolledFactors.some(
        (factor) => factor.factorId === "phone"
      );
      return hasSmsFactor;
    } catch (error) {
      console.error("Error checking SMS enrollment:", error);
      return false;
    }
  };

  mfaSmsCreate = async (phoneNumber: string) => {
    try {
      if (this.recaptchaVerifier == null) {
        this.recaptchaVerifier = new RecaptchaVerifier(
          this.auth,
          "auth-recaptcha",
          {
            size: "invisible",
            callback: async (response: any) => {
              // reCAPTCHA solved, you can proceed with
              // phoneAuthProvider.verifyPhoneNumber(...).
              // onSolvedRecaptcha();
              // console.log("SUCCESS");
            },
          }
        );
      }

      if (this.auth.currentUser == null || this.recaptchaVerifier == null) {
        return {
          success: false,
          error:
            "No user is currently signed in or reCAPTCHA verifier is not initialized.",
        };
      }

      const session = await multiFactor(this.auth.currentUser).getSession();
      // Specify the phone number and pass the MFA session.
      const phoneInfoOptions = {
        phoneNumber: phoneNumber,
        session: session,
      };

      const phoneAuthProvider = new PhoneAuthProvider(this.auth);
      this.smsVerificationId = await phoneAuthProvider.verifyPhoneNumber(
        phoneInfoOptions,
        this.recaptchaVerifier
      );
      return {
        success: true,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
        data:
          (error as any)?.code === "auth/requires-recent-login"
            ? "re-login"
            : undefined,
      };
    }
  };

  mfaSmsEnroll = async (code: string) => {
    try {
      if (this.smsVerificationId == null) {
        return {
          success: false,
          error: "No SMS verification ID available.",
        };
      }
      if (this.auth.currentUser == null) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }

      // Ask user for the verification code. Then:
      const cred = PhoneAuthProvider.credential(this.smsVerificationId, code);
      const multiFactorAssertion = PhoneMultiFactorGenerator.assertion(cred);
      await multiFactor(this.auth.currentUser).enroll(
        multiFactorAssertion,
        this.smsPhoneNumber ?? "Phone Number"
      );
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

  mfaSmsUnenroll = async () => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }

      const multiFactorUser = multiFactor(currentUser);
      const enrolledFactors = multiFactorUser.enrolledFactors;
      const smsFactor = enrolledFactors.find(
        (factor) => factor.factorId === "phone"
      );

      if (!smsFactor) {
        return {
          success: false,
          error: "No SMS factor found.",
        };
      }

      await multiFactorUser.unenroll(smsFactor);
      return {
        success: true,
      };
    } catch (error) {
      console.error("Error unenrolling SMS:", error);
      return {
        success: false,
        error: this.friendlyError(error),
        data:
          (error as any)?.code === "auth/requires-recent-login"
            ? "re-login"
            : undefined,
      };
    }
  };

  mfaSmsSend = async () => {
    if (!this.mfaResolver) {
      return {
        success: false,
        error: "No multi-factor resolver available.",
      };
    }

    const hint = this.mfaResolver.hints.find(
      (info) => info.factorId === "phone"
    );
    if (!hint) {
      return {
        success: false,
        error: "No Phone factor found.",
      };
    }

    if (this.recaptchaVerifier == null) {
      this.recaptchaVerifier = new RecaptchaVerifier(
        this.auth,
        "auth-recaptcha",
        {
          size: "invisible",
          callback: async (response: any) => {
            // reCAPTCHA solved, you can proceed with
            // phoneAuthProvider.verifyPhoneNumber(...).
            // onSolvedRecaptcha();
            console.log("SUCCESS");
          },
        }
      );
    }

    try {
      const phoneInfoOptions = {
        multiFactorHint: hint,
        session: this.mfaResolver.session,
      };

      // Send SMS verification code.
      const phoneAuthProvider = new PhoneAuthProvider(this.auth);
      this.smsVerificationId = await phoneAuthProvider.verifyPhoneNumber(
        phoneInfoOptions,
        this.recaptchaVerifier
      );

      return {
        success: true,
      };
    } catch (error) {
      this.recaptchaVerifier.clear();
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  mfaSmsVerify = async (code: string) => {
    if (!this.mfaResolver) {
      return {
        success: false,
        error: "No multi-factor resolver available.",
      };
    }
    if (!this.smsVerificationId) {
      return {
        success: false,
        error: "No SMS verification ID available.",
      };
    }

    try {
      const cred = PhoneAuthProvider.credential(this.smsVerificationId, code);

      const multiFactorAssertion = PhoneMultiFactorGenerator.assertion(cred);

      // Complete sign-in. This will also trigger the Auth state listeners.
      const userCredential = await this.mfaResolver.resolveSignIn(
        multiFactorAssertion
      );

      this.isMfaRequired = false;
      if (this.authStateCallback) {
        this.authStateCallback(this.auth.currentUser != null);
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

  changePassword = async (currentPassword: string, newPassword: string) => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser || !currentUser.email) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }

      // Re-authenticate the user with their current password
      const credential = EmailAuthProvider.credential(
        currentUser.email,
        currentPassword
      );

      try {
        await reauthenticateWithCredential(currentUser, credential);
      } catch (reauthError) {
        return {
          success: false,
          error: this.friendlyError(reauthError),
        };
      }

      // Update to the new password
      await updatePassword(currentUser, newPassword);

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

  sendEmailVerification = async (settings?: any) => {
    try {
      const currentUser = this.auth.currentUser;
      if (!currentUser) {
        return {
          success: false,
          error: "No user is currently signed in.",
        };
      }

      await sendEmailVerification(currentUser, settings as ActionCodeSettings);

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

  sendPasswordResetEmail = async (emailAddress: string) => {
    try {
      await sendPasswordResetEmail(this.auth, emailAddress);

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

  verifyPasswordResetCode = async (oobCode: string) => {
    try {
      const email = await verifyPasswordResetCode(this.auth, oobCode);

      return {
        success: true,
        data: email,
      };
    } catch (error) {
      return {
        success: false,
        error: this.friendlyError(error),
      };
    }
  };

  confirmPasswordReset = async (oobCode: string, newPassword: string) => {
    try {
      await confirmPasswordReset(this.auth, oobCode, newPassword);

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

  private friendlyError = (error: any): string => {
    this.firebaseError = error;
    const code = error.code;
    console.error(code);
    if (code === "auth/multi-factor-auth-required") {
      this.isMfaRequired = true;
      this.mfaResolver = getMultiFactorResolver(this.auth, this.firebaseError);
      if (this.authStateCallback) {
        this.authStateCallback(this.auth.currentUser != null);
      }
      return "";
    }
    switch (code) {
      case "auth/admin-restricted-operation":
        return "This action is restricted to administrators.";
      case "auth/argument-error":
        return "There was an issue with one of the input arguments.";
      case "auth/app-not-authorized":
        return "This app is not authorized to use Firebase Authentication.";
      case "auth/app-not-installed":
        return "The app needed to complete the sign-in is not installed.";
      case "auth/captcha-check-failed":
        return "Failed to verify you're not a robot. Please try again.";
      case "auth/code-expired":
        return "The code has expired. Please request a new one.";
      case "auth/cordova-not-ready":
        return "The app is not ready yet. Please try again shortly.";
      case "auth/cors-unsupported":
        return "This browser does not support Cross-Origin requests.";
      case "auth/credential-already-in-use":
        return "This credential is already associated with another account.";
      case "auth/custom-token-mismatch":
        return "The custom token is invalid or mismatched.";
      case "auth/requires-recent-login":
        return "Please log in again to complete this action.";
      case "auth/dependent-sdk-initialized-before-auth":
        return "There was a configuration error. Please reload the app.";
      case "auth/dynamic-link-not-activated":
        return "Dynamic link has not been activated.";
      case "auth/email-change-needs-verification":
        return "Please verify your email before changing it.";
      case "auth/email-already-in-use":
        return "This email is already in use by another account.";
      case "auth/emulator-config-failed":
        return "Failed to configure emulator. Please check the setup.";
      case "auth/expired-action-code":
        return "The action code has expired. Please request a new one.";
      case "auth/cancelled-popup-request":
        return "Multiple popup requests. Please try again.";
      case "auth/internal-error":
        return "An unexpected error occurred. Please try again.";
      case "auth/invalid-api-key":
        return "The API key is invalid. Please check your configuration.";
      case "auth/invalid-app-credential":
        return "Invalid app credentials provided.";
      case "auth/invalid-app-id":
        return "Invalid app ID. Please verify your app configuration.";
      case "auth/invalid-user-token":
      case "auth/user-token-expired":
        return "Your session has expired. Please log in again.";
      case "auth/invalid-auth-event":
        return "Invalid authentication event.";
      case "auth/invalid-cert-hash":
        return "Invalid certificate hash.";
      case "auth/invalid-verification-code":
        return "The verification code is invalid.";
      case "auth/invalid-continue-uri":
        return "Invalid continue URL.";
      case "auth/invalid-cordova-configuration":
        return "Invalid Cordova configuration.";
      case "auth/invalid-custom-token":
        return "The custom token format is incorrect.";
      case "auth/invalid-dynamic-link-domain":
        return "The dynamic link domain is not configured.";
      case "auth/invalid-email":
        return "The email address is not valid.";
      case "auth/invalid-emulator-scheme":
        return "Invalid emulator scheme.";
      case "auth/invalid-credential":
        return "Invalid credentials provided.";
      case "auth/invalid-message-payload":
        return "Invalid message payload in request.";
      case "auth/invalid-multi-factor-session":
        return "Invalid multi-factor session.";
      case "auth/invalid-oauth-client-id":
        return "The OAuth client ID is invalid.";
      case "auth/invalid-oauth-provider":
        return "The OAuth provider is invalid.";
      case "auth/invalid-action-code":
        return "The action code is invalid or expired.";
      case "auth/unauthorized-domain":
        return "This domain is not authorized to perform this operation.";
      case "auth/wrong-password":
        return "The password is incorrect.";
      case "auth/missing-password":
        return "Password is required.";
      case "auth/invalid-persistence-type":
        return "The persistence type is invalid.";
      case "auth/invalid-phone-number":
        return "The phone number format is incorrect.";
      case "auth/invalid-provider-id":
        return "The provider ID is not recognized.";
      case "auth/invalid-recipient-email":
        return "The recipient email is invalid.";
      case "auth/invalid-sender":
        return "The sender email is invalid.";
      case "auth/invalid-verification-id":
        return "The verification ID is invalid.";
      case "auth/invalid-tenant-id":
        return "Invalid tenant ID.";
      case "auth/multi-factor-info-not-found":
        return "Multi-factor info not found.";
      case "auth/multi-factor-auth-required":
        return "Multi-factor authentication is required.";
      case "auth/missing-android-pkg-name":
        return "Missing Android package name.";
      case "auth/missing-app-credential":
        return "Missing app credential.";
      case "auth/auth-domain-config-required":
        return "Authentication domain configuration is required.";
      case "auth/missing-verification-code":
        return "Missing verification code.";
      case "auth/missing-continue-uri":
        return "Missing continue URL.";
      case "auth/missing-iframe-start":
        return "Internal iframe error. Please reload the app.";
      case "auth/missing-ios-bundle-id":
        return "Missing iOS bundle ID.";
      case "auth/missing-or-invalid-nonce":
        return "Missing or invalid nonce.";
      case "auth/missing-multi-factor-info":
        return "Missing multi-factor information.";
      case "auth/missing-multi-factor-session":
        return "Missing multi-factor session.";
      case "auth/missing-phone-number":
        return "Missing phone number.";
      case "auth/missing-verification-id":
        return "Missing verification ID.";
      case "auth/app-deleted":
        return "This app instance was deleted.";
      case "auth/account-exists-with-different-credential":
        return "An account exists with a different credential. Try using that provider.";
      case "auth/network-request-failed":
        return "Network error. Please check your connection.";
      case "auth/null-user":
        return "No user found.";
      case "auth/no-auth-event":
        return "No authentication event detected.";
      case "auth/no-such-provider":
        return "This provider is not supported.";
      case "auth/operation-not-allowed":
        return "This operation is not allowed. Please contact support.";
      case "auth/operation-not-supported-in-this-environment":
        return "This operation is not supported in this environment.";
      case "auth/popup-blocked":
        return "The popup was blocked by your browser.";
      case "auth/popup-closed-by-user":
        return "The popup was closed before completing the sign-in.";
      case "auth/provider-already-linked":
        return "This provider is already linked to another account.";
      case "auth/quota-exceeded":
        return "Quota exceeded. Please try again later.";
      case "auth/redirect-cancelled-by-user":
        return "The redirect was canceled.";
      case "auth/redirect-operation-pending":
        return "A redirect operation is already in progress.";
      case "auth/rejected-credential":
        return "The credential was rejected.";
      case "auth/second-factor-already-in-use":
        return "This second factor is already in use.";
      case "auth/maximum-second-factor-count-exceeded":
        return "Maximum number of second factors reached.";
      case "auth/tenant-id-mismatch":
        return "Tenant ID does not match.";
      case "auth/timeout":
        return "The operation timed out. Please try again.";
      case "auth/totp-challenge-timeout":
        return "Your authenticator code has expired. Logout and log back in to try again.";
      case "auth/too-many-requests":
        return "You've made too many attempts. Please wait a few minutes before trying again.";
      case "auth/unauthorized-continue-uri":
        return "The continue URL is not authorized.";
      case "auth/unsupported-first-factor":
        return "Unsupported first factor.";
      case "auth/unsupported-persistence-type":
        return "Unsupported persistence type.";
      case "auth/unsupported-tenant-operation":
        return "This tenant operation is not supported.";
      case "auth/unverified-email":
        return "Your email is not verified.";
      case "auth/user-cancelled":
        return "The operation was cancelled by the user.";
      case "auth/user-not-found":
        return "No user found with this email.";
      case "auth/user-disabled":
        return "This user has been disabled.";
      case "auth/user-mismatch":
        return "The user does not match the provided credentials.";
      case "auth/user-signed-out":
        return "User is signed out. Please sign in again.";
      case "auth/weak-password":
        return "Your password is too weak.";
      case "auth/web-storage-unsupported":
        return "Web storage is not supported in this browser.";
      case "auth/already-initialized":
        return "Auth is already initialized.";
      case "auth/recaptcha-not-enabled":
        return "reCAPTCHA is not enabled.";
      case "auth/missing-recaptcha-token":
        return "Missing reCAPTCHA token.";
      case "auth/invalid-recaptcha-token":
        return "Invalid reCAPTCHA token.";
      case "auth/invalid-recaptcha-action":
        return "Invalid reCAPTCHA action.";
      case "auth/missing-client-type":
        return "Missing client type for reCAPTCHA.";
      case "auth/missing-recaptcha-version":
        return "Missing reCAPTCHA version.";
      case "auth/invalid-recaptcha-version":
        return "Invalid reCAPTCHA version.";
      case "auth/invalid-req-type":
        return "Invalid request type.";
      case "auth/invalid-hosting-link-domain":
        return "Invalid domain used for hosting link.";
      default:
        return "An unexpected error occurred. Please try again.";
    }
  };
}
