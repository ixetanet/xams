import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";
import { Loader, Alert, Button } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import React from "react";
import { AuthProvider, useAuth } from "@ixeta/headless-auth-react";
import { firebaseApp, initializeFirebase, firebaseAuthConfig } from "./_app";
import { useRouter } from "next/router";
import LoginContainer from "@/components/auth/LoginContainer";
import ResetPasswordForm from "@/components/auth/login/ResetPasswordForm";
import { LoginProvider } from "@/components/auth/LoginContext";

const ResetPasswordContent = () => {
  const router = useRouter();
  const { oobCode } = router.query;
  const auth = useAuth();

  const authQuery = useQuery<FirebaseConfig>({
    queryKey: ["auth-settings"],
    queryFn: async () => {
      const resp = await fetch(
        `${process.env.NEXT_PUBLIC_API}${API_CONFIG}?name=firebase`
      );
      if (!resp.ok) {
        return null;
      }
      return resp.json();
    },
  });

  if (authQuery.isLoading || !router.isReady || !auth.isReady) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  if (!authQuery.data) {
    return (
      <LoginContainer>
        <Alert color="red" variant="light">
          Error loading auth settings
        </Alert>
      </LoginContainer>
    );
  }

  if (!oobCode) {
    return (
      <LoginContainer>
        <Alert color="red" variant="light">
          Invalid password reset link. No reset code provided.
        </Alert>
        <Button onClick={() => router.push("/login")} fullWidth mt="md">
          Return to Login
        </Button>
      </LoginContainer>
    );
  }

  return (
    <LoginProvider
      providers={authQuery.data.providers}
      auth={auth}
      smsEnrollmentEnabled={authQuery.data.enableSmsMfa}
      redirectUrl={authQuery.data.redirectUrl}
    >
      <LoginContainer>
        <ResetPasswordForm oobCode={oobCode as string} />
      </LoginContainer>
    </LoginProvider>
  );
};

const ResetPassword = () => {
  const authQuery = useQuery<FirebaseConfig>({
    queryKey: ["auth-settings"],
    queryFn: async () => {
      const resp = await fetch(
        `${process.env.NEXT_PUBLIC_API}${API_CONFIG}?name=firebase`
      );
      if (!resp.ok) {
        return null;
      }
      return resp.json();
    },
  });

  if (authQuery.isLoading) {
    return (
      <LoginContainer>
        <div className="w-full h-full flex justify-center items-center">
          <Loader />
        </div>
      </LoginContainer>
    );
  }

  // Initialize Firebase if not already initialized
  if (
    authQuery.data &&
    Object.keys(authQuery.data).length !== 0 &&
    firebaseApp === null
  ) {
    initializeFirebase(authQuery.data);
  }

  if (firebaseAuthConfig == null) {
    return (
      <LoginContainer>
        <div className="w-full h-full flex justify-center items-center">
          <Loader />
        </div>
      </LoginContainer>
    );
  }

  return (
    <AuthProvider authConfig={firebaseAuthConfig}>
      <ResetPasswordContent />
    </AuthProvider>
  );
};

export default ResetPassword;
