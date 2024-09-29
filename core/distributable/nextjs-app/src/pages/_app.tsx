import "@/styles/globals.css";
import "@ixeta/xams/dist/styles.css";
import "@ixeta/xams/dist/global.css";
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
import { useState } from "react";
import { getCookie, setCookie } from "cookies-next";
import { useRouter } from "next/router";

export default function App(props: AppProps & { colorScheme: ColorScheme }) {
  const router = useRouter();
  const userId = getQueryParam("userid", router.asPath);
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
            UserId: userId as string,
          }}
        >
          <AppContextProvider>
            <Component {...pageProps} />
          </AppContextProvider>
        </AuthContextProvider>
      </MantineProvider>
    </ColorSchemeProvider>
  );
}

App.getInitialProps = async (appContext: AppContext) => {
  const appProps = await NextApp.getInitialProps(appContext);
  return {
    ...appProps,
    colorScheme: getCookie("mantine-color-scheme", appContext.ctx) || "light",
  };
};
