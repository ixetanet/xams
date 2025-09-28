import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";
import { Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import React from "react";
import { AuthProvider } from "@ixeta/headless-auth-react";
import { FirebaseAuthConfig } from "@ixeta/headless-auth-react-firebase";
import ProfileComponent from "@/components/profile/ProfileComponent";
import { firebaseApp, initializeFirebase, firebaseAuthConfig } from "../_app";

const Profile = () => {
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
      <ProfileComponent providers={authQuery.data.providers} />
    </AuthProvider>
  );
};

export default Profile;
