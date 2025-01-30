import "@mantine/core/styles.css";
import "@mantine/tiptap/styles.css";
import "@/styles/globals.css";
import "@ixeta/xams/dist/styles.css";
import "@ixeta/xams/dist/global.css";
import { AppProps } from "next/app";
import {
  AppContextProvider,
  AuthContextProvider,
  getQueryParam,
} from "@ixeta/xams";
import { MantineProvider } from "@mantine/core";
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
