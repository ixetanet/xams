import { useAuth } from "@ixeta/headless-auth-react";
import { Loader } from "@mantine/core";
import React, { useEffect } from "react";
import { LoginProvider } from "./LoginContext";
import EmailVerificationForm from "./EmailVerificationForm";
import LoginForm from "./LoginForm";
import MfaSelectionForm from "./MfaSelectionForm";
import MfaSmsForm from "./MfaSmsForm";
import MfaTotpForm from "./MfaTotpForm";
import RegisterForm from "./RegisterForm";
import ProfileMainView from "../profile/ProfileMainView";
import SetupSmsView from "../profile/SetupSmsView";
import SetupTotpView from "../profile/SetupTotpView";

interface LoginComponentProps {
  defaultView: string;
  onLoginSuccess?: () => void;
  providers: string[];
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

  if (!auth.isLoggedIn && auth.view === "profile") {
    auth.setView("login");
  }

  return (
    <LoginProvider providers={props.providers} auth={auth}>
      {/* User is attempting to login and MFA is required */}
      {auth.isMfaRequired && (
        <>
          {/* {auth.view !== "mfa_totp" && auth.view !== "mfa_sms" && (
            <MfaSelectionForm />
          )} */}
          {/* Disable SMS MFA */}
          {/* {auth.view === "mfa_totp" && <MfaTotpForm />} */}
          <MfaTotpForm />
          {/* {auth.view === "mfa_sms" && <MfaSmsForm />} */}
        </>
      )}

      {/* The user is not logged in or a re-login is required for MFA */}
      {(!auth.isLoggedIn || auth.isReLoginRequired) && !auth.isMfaRequired && (
        <>
          {auth.view === "login" && <LoginForm />}
          {auth.view === "register" && <RegisterForm />}
        </>
      )}

      {/* Email verification required */}
      {auth.isLoggedIn && !auth.isEmailVerified && <EmailVerificationForm />}

      {auth.view === "setup_mfa_totp" && <SetupTotpView />}
      {auth.view === "setup_mfa_sms" && <SetupSmsView />}
      {/* Don't allow SMS Enrollment */}
      {/* {auth.view === "setup_mfa_sms_enroll" && <SetupSmsEnrollView />} */}
      {auth.view === "profile" && <ProfileMainView />}
    </LoginProvider>
  );
};

export default LoginComponent;
