import { AdminDashboard, API_CONFIG } from "@ixeta/xams";
import { Button, Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import { getAuth } from "firebase/auth";
import { FirebaseConfig } from "@/types";
import { useRouter } from "next/router";
import { firebaseApp, initializeFirebase } from "./_app";
import { FirebaseApp, initializeApp } from "firebase/app";

export default function Home() {
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

  // If there are auth settings
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp != null) {
    const fireBaseAuth = getAuth(firebaseApp);
    if (fireBaseAuth.currentUser === null) {
      router.push("/auth/login");
      return <></>;
    }
  }

  return (
    <AdminDashboard
      accessDeniedMessage={
        <div className="w-full h-full flex flex-col gap-2 justify-center items-center">
          You don&apos;t have permission to view this page. Please contact your
          system administrator.
          {firebaseApp != null && (
            <Button
              onClick={() => {
                getAuth(firebaseApp as FirebaseApp).signOut();
                router.push("/");
              }}
            >
              Logout
            </Button>
          )}
        </div>
      }
    />
  );
}
