import { AdminDashboard, API_CONFIG } from "@ixeta/xams";
import { Loader } from "@mantine/core";
import { useQuery } from "@tanstack/react-query";
import { FirebaseApp, initializeApp } from "firebase/app";
import { getAuth, sendEmailVerification } from "firebase/auth";
import { FirebaseAuthConfig } from "@ixeta/headless-auth-react-firebase";
import { FirebaseConfig } from "@/types";
import { useRouter } from "next/router";

export let firebaseApp: FirebaseApp | null = null;
export let firebaseAuthConfig: FirebaseAuthConfig | null = null;

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

  // If there are firebase auth settings, go to login screen
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp === null) {
    firebaseApp = initializeApp(authQuery.data);
    const fireBaseAuth = getAuth(firebaseApp);
    firebaseAuthConfig = new FirebaseAuthConfig(fireBaseAuth);
    firebaseAuthConfig.setOptions({
      totpAppName: authQuery.data.projectId,
      onSignUpSuccess: async (authConfig) => {
        if (fireBaseAuth.currentUser) {
          await sendEmailVerification(fireBaseAuth.currentUser);
        }
      },
      onSignInSuccess: async () => {
        // router.push("/app/coupons");
      },
      onSignOutSuccess: async () => {
        // router.push("/");
      },
    });
  }

  // If there are auth settings
  if (Object.keys(authQuery.data).length !== 0 && firebaseApp != null) {
    const fireBaseAuth = getAuth(firebaseApp);
    if (fireBaseAuth.currentUser === null) {
      router.push("/auth/login");
      return <></>;
    }
    return <div>No auth settings found</div>;
  }

  return <AdminDashboard />;
}
