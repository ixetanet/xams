import "@mantine/core/styles.css";
import "@mantine/tiptap/styles.css";
import "@/styles/globals.css";
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";
import { AppProps } from "next/app";
import { AppContextProvider, AuthContextProvider } from "@ixeta/xams";
import { MantineProvider } from "@mantine/core";
import { SessionProvider } from "next-auth/react";

export default function App(props: AppProps) {
  const {
    Component,
    pageProps: { session, ...pageProps },
  } = props;

  return (
    <SessionProvider session={session}>
      <MantineProvider
        theme={{
          primaryColor: "indigo",
        }}
      >
        <AuthContextProvider
          apiUrl={process.env.NEXT_PUBLIC_API as string}
          headers={{
            UserId: "f8a43b04-4752-4fda-a89f-62bebcd8240c" as string,
          }}
        >
          <AppContextProvider>
            <Component {...pageProps} />
          </AppContextProvider>
        </AuthContextProvider>
      </MantineProvider>
    </SessionProvider>
  );
}
