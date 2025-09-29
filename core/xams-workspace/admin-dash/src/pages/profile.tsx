import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";
import { Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import React from "react";
import { AuthProvider } from "@ixeta/headless-auth-react";
import LoginComponent from "@/components/auth/LoginComponent";
import { useRouter } from "next/router";
import { firebaseApp, initializeFirebase, firebaseAuthConfig } from "./_app";

const Profile = () => {
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
    return <div>Error loading auth settings</div>;
  }

  // Initialize Firebase if not already initialized
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp === null) {
    initializeFirebase(authQuery.data);
  }

  if (firebaseAuthConfig == null) {
    return <div>Loading...</div>;
  }

  return (
    <AuthProvider authConfig={firebaseAuthConfig}>
      <LoginComponent
        providers={authQuery.data.providers}
        // onLoginSuccess={() => router.push("/profile")}
        defaultView="profile"
        smsEnrollmentEnabled={authQuery.data.enableSmsMfa}
        emailVerificationRedirectUrl={
          authQuery.data.emailVerificationRedirectUrl
        }
      />
    </AuthProvider>
  );
};

export default Profile;
