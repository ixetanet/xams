import "@mantine/core/styles.css";
import "@/styles/globals.css";
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";
import React from "react";
import NextApp, { AppProps, AppContext } from "next/app";
import {
  AppContextProvider,
  AuthContextProvider,
  getQueryParam,
} from "@ixeta/xams";
import { MantineProvider } from "@mantine/core";
import { useState } from "react";
import { getCookie, setCookie } from "cookies-next";
import { useRouter } from "next/router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import { FirebaseApp, initializeApp } from "firebase/app";
import { Auth, getAuth, sendEmailVerification } from "firebase/auth";
import { FirebaseAuthConfig } from "@ixeta/headless-auth-react-firebase";
import { FirebaseConfig } from "@/types";
import { verifyEmail } from "@/utils/verifyEmailUtil";

export let firebaseApp: FirebaseApp | null = null;
export let firebaseAuthConfig: FirebaseAuthConfig;
export let firebaseAuth: Auth | null = null;
export const initializeFirebase = (config: FirebaseConfig) => {
  firebaseApp = initializeApp(config);
  firebaseAuth = getAuth(firebaseApp);
  firebaseAuthConfig = new FirebaseAuthConfig(firebaseAuth);
  firebaseAuthConfig.setOptions({
    totpAppName: config.projectId,
    onSignUpSuccess: async (authConfig) => {},
    onSignInSuccess: async () => {
      if (firebaseApp == null) return;
      firebaseAuth = getAuth(firebaseApp);
      if (firebaseAuth?.currentUser != null) {
        await verifyEmail(firebaseAuth?.currentUser?.email ?? "");
      }
    },
    onSignOutSuccess: async () => {},
  });
};

const queryClient = new QueryClient();

export default function App(props: AppProps) {
  const router = useRouter();
  const userId = getQueryParam("userid", router.asPath);
  const {
    Component,
    pageProps: { session, ...pageProps },
  } = props;

  return (
    <QueryClientProvider client={queryClient}>
      <MantineProvider
        theme={{
          primaryColor: "indigo",
        }}
      >
        <AuthContextProvider
          apiUrl={process.env.NEXT_PUBLIC_API as string}
          headers={{
            UserId: userId as string,
          }}
        >
          <AppContextProvider>
            <Component {...pageProps} />
            <div id="auth-recaptcha" className="invisible" />
          </AppContextProvider>
        </AuthContextProvider>
      </MantineProvider>
    </QueryClientProvider>
  );
}
