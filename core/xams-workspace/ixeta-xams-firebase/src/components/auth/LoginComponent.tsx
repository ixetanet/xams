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
import { getQueryParam } from "@ixeta/xams";

interface LoginComponentProps {
  defaultView: string;
  providers: string[];
  smsEnrollmentEnabled?: boolean;
  redirectUrls: string[];
  fallbackRedirectUrl?: string; // No redirect url provided in query param
}

export const LoginComponent = (props: LoginComponentProps) => {
  const auth = useAuth();
  const redirectUrl = getQueryParam("redirecturl");

  useEffect(() => {
    if (auth.isLoggedIn && auth.isEmailVerified && !auth.isMfaRequired) {
      const authRedirectUrl = localStorage.getItem("auth-redirecturl");
      if (authRedirectUrl != null && authRedirectUrl !== "") {
        localStorage.removeItem("auth-redirecturl");
        window.location.href = authRedirectUrl;
      } else {
        auth.setView(props.defaultView);
      }
    }
  }, [auth.isLoggedIn, auth.isEmailVerified, auth.isMfaRequired]);

  // Validate redirecturl
  if (
    redirectUrl != null &&
    redirectUrl !== "" &&
    !props.redirectUrls.includes(redirectUrl)
  ) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        Invalid redirect URL
      </div>
    );
  }

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  // If a redirect url is provided, store it in localStorage for use after login
  // If no redirect url is provided, check storage for one
  // If there's still none, use the fallback url
  // When the redirect happens, the stored url is cleared
  if (redirectUrl != null && redirectUrl !== "") {
    localStorage.setItem("auth-redirecturl", redirectUrl);
  }
  const authRedirectUrl = localStorage.getItem("auth-redirecturl");
  if (authRedirectUrl == null && props.fallbackRedirectUrl) {
    if (window.location.port !== "") {
      // Include port in localhost
      localStorage.setItem(
        "auth-redirecturl",
        `${window.location.protocol}//${window.location.hostname}:${window.location.port}${props.fallbackRedirectUrl}`
      );
    } else {
      localStorage.setItem(
        "auth-redirecturl",
        `${window.location.protocol}//${window.location.hostname}${props.fallbackRedirectUrl}`
      );
    }
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

  // Logged in and everything is set up, redirect occurs in useEffect
  if (
    auth.isLoggedIn &&
    auth.isEmailVerified &&
    !auth.isMfaRequired &&
    authRedirectUrl
  ) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  return (
    <LoginProvider
      providers={props.providers}
      auth={auth}
      smsEnrollmentEnabled={props.smsEnrollmentEnabled ?? false}
      redirectUrls={props.redirectUrls}
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
