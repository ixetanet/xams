import { AdminDashboard, API_CONFIG } from "@ixeta/xams";
import { Button, Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import { FirebaseConfig } from "@/types";
import { firebaseApp, firebaseAuthConfig, initializeFirebase } from "./_app";
import AuthAdminDashboard from "@/components/AuthAdminDashboard";
import { AuthProvider } from "@ixeta/headless-auth-react";

export default function Home() {
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

  // If there are auth settings
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp != null) {
    return (
      <AuthProvider authConfig={firebaseAuthConfig}>
        <AuthAdminDashboard />
      </AuthProvider>
    );
  }

  return <AdminDashboard />;
}
