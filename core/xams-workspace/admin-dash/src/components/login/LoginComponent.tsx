import { useAuth } from "@ixeta/headless-auth-react";
import { Loader } from "@mantine/core";
import { useRouter } from "next/router";
import React from "react";
import { LoginProvider } from "./LoginContext";
import EmailVerificationForm from "./EmailVerificationForm";
import LoginForm from "./LoginForm";
import MfaSelectionForm from "./MfaSelectionForm";
import MfaSmsForm from "./MfaSmsForm";
import MfaTotpForm from "./MfaTotpForm";
import RegisterForm from "./RegisterForm";

interface LoginComponentProps {
  providers: string[];
}

const LoginComponent = (props: LoginComponentProps) => {
  const router = useRouter();
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  // Login success - redirect to dashboard
  if (auth.isLoggedIn && auth.isEmailVerified) {
    router.push("/");
    return <></>;
  }

  return (
    <LoginProvider providers={props.providers}>
      {/* User is attempting to login and MFA is required */}
      {!auth.isLoggedIn && auth.isMfaRequired && (
        <>
          {auth.view !== "mfa_totp" && auth.view !== "mfa_sms" && (
            <MfaSelectionForm />
          )}
          {auth.view === "mfa_totp" && <MfaTotpForm />}
          {auth.view === "mfa_sms" && <MfaSmsForm />}
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
    </LoginProvider>
  );
};

export default LoginComponent;
