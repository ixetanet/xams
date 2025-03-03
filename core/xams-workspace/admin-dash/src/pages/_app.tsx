import "@mantine/core/styles.css";
import "@/styles/globals.css";
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";
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

export default function App(props: AppProps) {
  const router = useRouter();
  const userId = getQueryParam("userid", router.asPath);
  const {
    Component,
    pageProps: { session, ...pageProps },
  } = props;

  return (
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
        </AppContextProvider>
      </AuthContextProvider>
    </MantineProvider>
  );
}
