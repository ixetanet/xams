import "@/styles/globals.css";
import "@ixeta/xams/styles.css";
import "@ixeta/xams/global.css";
import NextApp, { AppProps, AppContext } from "next/app";
import {
  AppContextProvider,
  AuthContextProvider,
  getQueryParam,
} from "@ixeta/xams";
import {
  ColorScheme,
  ColorSchemeProvider,
  MantineProvider,
} from "@mantine/core";
import { SessionProvider } from "next-auth/react";
import { useState } from "react";
import { getCookie, setCookie } from "cookies-next";

export default function App(props: AppProps & { colorScheme: ColorScheme }) {
  const {
    Component,
    pageProps: { session, ...pageProps },
  } = props;
  const [colorScheme, setColorScheme] = useState<ColorScheme>(
    props.colorScheme
  );
  const primaryColor = colorScheme === "dark" ? "indigo" : "indigo";

  const toggleColorScheme = (value?: ColorScheme) => {
    const nextColorScheme =
      value || (colorScheme === "dark" ? "light" : "dark");
    setColorScheme(nextColorScheme);
    setCookie("mantine-color-scheme", nextColorScheme, {
      maxAge: 60 * 60 * 24 * 30,
    });
  };

  return (
    <SessionProvider session={session}>
      <ColorSchemeProvider
        colorScheme={colorScheme}
        toggleColorScheme={toggleColorScheme}
      >
        <MantineProvider
          withGlobalStyles
          withNormalizeCSS
          theme={{
            colorScheme,
            primaryColor: primaryColor,
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
      </ColorSchemeProvider>
    </SessionProvider>
  );
}

App.getInitialProps = async (appContext: AppContext) => {
  const appProps = await NextApp.getInitialProps(appContext);
  return {
    ...appProps,
    colorScheme: getCookie("mantine-color-scheme", appContext.ctx) || "light",
  };
};
