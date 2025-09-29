import React from "react";
import { Loader } from "@mantine/core";
import { useAuth } from "@ixeta/headless-auth-react";
import { useRouter } from "next/router";
import { LoginProvider } from "../login/LoginContext";
import LoginForm from "../login/LoginForm";
import { ProfileProvider } from "./ProfileContext";
import ProfileMainView from "./ProfileMainView";
import SetupTotpView from "./SetupTotpView";
import SetupSmsView from "./SetupSmsView";
import SetupSmsEnrollView from "./SetupSmsEnrollView";
import LoginComponent from "../login/LoginComponent";

interface ProfileComponentProps {
  providers: string[];
}

const ProfileComponent = ({ providers }: ProfileComponentProps) => {
  const auth = useAuth({
    defaultView: "profile",
  });
  const router = useRouter();

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  if (!auth.isLoggedIn) {
    router.push("/auth/login");
  }

  // Relogin required - for MFA changes
  if (auth.view === "login") {
    return (
      <LoginProvider providers={providers} auth={auth}>
        <LoginComponent
          providers={providers}
          onLoginSuccess={() => auth.setView("profile")}
        />
      </LoginProvider>
    );
  }

  return (
    <ProfileProvider providers={providers} auth={auth}>
      {auth.view === "setup_mfa_totp" && <SetupTotpView />}
      {auth.view === "setup_mfa_sms" && <SetupSmsView />}
      {/* Don't allow SMS Enrollment */}
      {/* {auth.view === "setup_mfa_sms_enroll" && <SetupSmsEnrollView />} */}
      {auth.view === "profile" && <ProfileMainView />}
    </ProfileProvider>
  );
};

export default ProfileComponent;
