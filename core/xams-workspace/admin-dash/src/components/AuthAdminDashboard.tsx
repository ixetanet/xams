import { firebaseApp, firebaseAuth } from "@/pages/_app";
import {
  AuthContextProvider,
  AdminDashboard,
  AppContextProvider,
} from "@ixeta/xams";
import {
  Avatar,
  Box,
  Button,
  Divider,
  Group,
  Loader,
  NavLink,
  Text,
} from "@mantine/core";
import router from "next/router";
import React from "react";
import { getAuth } from "firebase/auth";
import { FirebaseApp, initializeApp } from "firebase/app";
import { IconChevronRight, IconLogout } from "@tabler/icons-react";
import { AuthProvider, useAuth } from "@ixeta/headless-auth-react";

const AuthAdminDashboard = () => {
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  return (
    <AuthContextProvider
      apiUrl={process.env.NEXT_PUBLIC_API as string}
      getAccessToken={auth.authConfig.getAccessToken}
    >
      <AppContextProvider>
        <AdminDashboard
          addMenuItems={[
            {
              order: 10000,
              navLink: (
                <NavLink
                  label="Logout"
                  leftSection={<IconLogout size={16} />}
                  onClick={() => {
                    auth.signOut();
                    router.push("/");
                  }}
                />
              ),
            },
          ]}
          userCard={
            <Box>
              <Divider />
              <NavLink
                label={firebaseAuth?.currentUser?.email || "User"}
                leftSection={<Avatar size="sm" />}
                rightSection={<IconChevronRight size={14} stroke={1.5} />}
                onClick={() => router.push("/auth/profile")}
                p="md"
              />
            </Box>
          }
          accessDeniedMessage={
            <div className="w-full h-full flex flex-col gap-2 justify-center items-center">
              You don&apos;t have permission to view this page. Please contact
              your system administrator.
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
      </AppContextProvider>
    </AuthContextProvider>
  );
};

export default AuthAdminDashboard;
