import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";
import { Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import React from "react";
import { AuthProvider } from "@ixeta/headless-auth-react";
import LoginComponent from "@/components/auth/LoginComponent";
import {
  firebaseApp,
  initializeFirebase,
  firebaseAuthConfig,
  firebaseAuth,
} from "./_app";
import { useRouter } from "next/router";
import { verifyEmail } from "@/utils/verifyEmailUtil";

const Login = () => {
  const router = useRouter();
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
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  if (!authQuery.data) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        Error loading auth settings
      </div>
    );
  }

  // Initialize Firebase if not already initialized
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp === null) {
    initializeFirebase(authQuery.data);
  }

  if (firebaseAuthConfig == null) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        Loading...
      </div>
    );
  }

  return (
    <AuthProvider authConfig={firebaseAuthConfig}>
      <LoginComponent
        providers={authQuery.data.providers}
        defaultView={"login"}
        smsEnrollmentEnabled={authQuery.data.enableSmsMfa}
        redirectUrls={authQuery.data.redirectUrls}
        fallbackRedirectUrl="/x"
      />
    </AuthProvider>
  );
};

export default Login;
