import { useAuth } from "@ixeta/headless-auth-react";
import { Loader } from "@mantine/core";
import React, { useEffect } from "react";
import { LoginProvider } from "./LoginContext";
import LoginContainer from "./LoginContainer";
import EmailVerificationForm from "./login/EmailVerificationForm";
import LoginForm from "./login/LoginForm";
import MfaSelectionForm from "./login/MfaSelectionForm";
import MfaSmsForm from "./login/MfaSmsForm";
import MfaTotpForm from "./login/MfaTotpForm";
import RegisterForm from "./login/RegisterForm";
import ForgotPasswordForm from "./login/ForgotPasswordForm";
import ProfileMainView from "./profile/ProfileMainView";
import SetupSmsView from "./profile/SetupSmsView";
import SetupTotpView from "./profile/SetupTotpView";
import SetupSmsEnrollView from "./profile/SetupSmsEnrollView";

interface LoginComponentProps {
  defaultView: string;
  onLoginSuccess?: () => void;
  providers: string[];
  smsEnrollmentEnabled?: boolean;
  redirectUrl: string;
}

const LoginComponent = (props: LoginComponentProps) => {
  const auth = useAuth();

  useEffect(() => {
    if (auth.isLoggedIn && auth.isEmailVerified && !auth.isMfaRequired) {
      if (props.onLoginSuccess) {
        props.onLoginSuccess();
      } else {
        auth.setView(props.defaultView);
      }
    }
  }, [auth.isLoggedIn, auth.isEmailVerified, auth.isMfaRequired]);

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  // Redirect to login if trying to access profile while not logged in
  if (!auth.isLoggedIn && auth.view === "profile") {
    auth.setView("login");
  }

  // Redirect to profile if already logged in and on login view
  if (
    !auth.isReLoginRequired &&
    auth.isLoggedIn &&
    auth.view === "login" &&
    auth.view !== props.defaultView
  ) {
    auth.setView(props.defaultView);
  }

  // If only one MFA method is available, skip selection step
  if (auth.mfaFactors.length === 1 && auth.isMfaRequired) {
    if (auth.mfaFactors[0] === "totp" && auth.view !== "mfa_totp") {
      auth.setView("mfa_totp");
    }
    if (auth.mfaFactors[0] === "sms" && auth.view !== "mfa_sms") {
      auth.setView("mfa_sms");
      auth.mfaSmsSend();
    }
  }

  return (
    <LoginProvider
      providers={props.providers}
      auth={auth}
      smsEnrollmentEnabled={props.smsEnrollmentEnabled ?? false}
      redirectUrl={props.redirectUrl}
    >
      <LoginContainer maxWidth={auth.view === "profile" ? "md" : "sm"}>
        {/* User is attempting to login and MFA is required */}
        {auth.isMfaRequired && (
          <>
            {auth.view !== "mfa_totp" && auth.view !== "mfa_sms" && (
              <MfaSelectionForm />
            )}
            {auth.view === "mfa_totp" && <MfaTotpForm />}
            {auth.view === "mfa_sms" && <MfaSmsForm />}
          </>
        )}

        {/* The user is not logged in or a re-login is required for MFA */}
        {(!auth.isLoggedIn || auth.isReLoginRequired) &&
          !auth.isMfaRequired && (
            <>
              {auth.view === "login" && <LoginForm />}
              {auth.view === "register" && <RegisterForm />}
              {auth.view === "forgot_password" && <ForgotPasswordForm />}
            </>
          )}

        {/* Email verification required */}
        {auth.isLoggedIn && !auth.isEmailVerified && <EmailVerificationForm />}

        {auth.isLoggedIn && auth.isEmailVerified && (
          <>
            {auth.view === "setup_mfa_totp" && <SetupTotpView />}
            {auth.view === "setup_mfa_sms" && <SetupSmsView />}
            {auth.view === "setup_mfa_sms_enroll" && <SetupSmsEnrollView />}
            {auth.view === "profile" && <ProfileMainView />}
          </>
        )}
      </LoginContainer>
    </LoginProvider>
  );
};

export default LoginComponent;
