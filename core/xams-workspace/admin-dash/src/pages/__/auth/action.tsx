import React, { useEffect, useState } from "react";
import { useRouter } from "next/router";
import { Container, Paper, Title, Text, Stack, Loader, Alert, Button } from "@mantine/core";
import { applyActionCode } from "firebase/auth";
import { firebaseAuth, initializeFirebase } from "../../_app";
import { useQuery } from "@tanstack/react-query";
import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";

type ActionMode = "verifyEmail" | "resetPassword" | "recoverEmail";

const AuthAction = () => {
  const router = useRouter();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [actionMode, setActionMode] = useState<ActionMode | null>(null);

  // Fetch Firebase config
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

  useEffect(() => {
    // Initialize Firebase if not already initialized
    if (authQuery.data && Object.keys(authQuery.data).length !== 0 && firebaseAuth === null) {
      initializeFirebase(authQuery.data);
    }
  }, [authQuery.data]);

  useEffect(() => {
    const handleAction = async () => {
      if (!router.isReady || !firebaseAuth) return;

      const { mode, oobCode, continueUrl } = router.query;

      if (!mode || !oobCode) {
        setStatus("error");
        setErrorMessage("Invalid verification link. Please try again.");
        return;
      }

      setActionMode(mode as ActionMode);

      try {
        switch (mode) {
          case "verifyEmail":
            await applyActionCode(firebaseAuth, oobCode as string);
            setStatus("success");

            // Redirect after 3 seconds
            setTimeout(() => {
              if (continueUrl) {
                window.location.href = continueUrl as string;
              } else {
                router.push("/login");
              }
            }, 3000);
            break;

          case "resetPassword":
            // Store oobCode and redirect to password reset page
            // This will be implemented when you add password reset functionality
            router.push(`/reset-password?oobCode=${oobCode}`);
            break;

          case "recoverEmail":
            await applyActionCode(firebaseAuth, oobCode as string);
            setStatus("success");
            setTimeout(() => {
              router.push("/login");
            }, 3000);
            break;

          default:
            setStatus("error");
            setErrorMessage("Unsupported action type.");
        }
      } catch (error: any) {
        setStatus("error");

        // Handle specific Firebase error codes
        if (error.code === "auth/invalid-action-code") {
          setErrorMessage("This link has expired or has already been used.");
        } else if (error.code === "auth/user-disabled") {
          setErrorMessage("This account has been disabled.");
        } else if (error.code === "auth/user-not-found") {
          setErrorMessage("No account found with this email.");
        } else {
          setErrorMessage(error.message || "An error occurred. Please try again.");
        }
      }
    };

    handleAction();
  }, [router.isReady, router.query, firebaseAuth]);

  // Loading state while fetching Firebase config
  if (authQuery.isLoading) {
    return (
      <Container size="xs" style={{ paddingTop: "100px" }}>
        <Paper p="xl" shadow="md" radius="md">
          <Stack align="center" gap="lg">
            <Loader size="lg" />
            <Text size="sm" c="dimmed">Loading...</Text>
          </Stack>
        </Paper>
      </Container>
    );
  }

  if (!authQuery.data) {
    return (
      <Container size="xs" style={{ paddingTop: "100px" }}>
        <Paper p="xl" shadow="md" radius="md">
          <Stack gap="lg">
            <Alert color="red" variant="light">
              Error loading authentication settings.
            </Alert>
            <Button onClick={() => router.push("/login")} fullWidth>
              Return to Login
            </Button>
          </Stack>
        </Paper>
      </Container>
    );
  }

  const getTitle = () => {
    if (status === "loading") return "Processing...";
    if (status === "error") return "Verification Failed";

    switch (actionMode) {
      case "verifyEmail":
        return "Email Verified!";
      case "recoverEmail":
        return "Email Recovered!";
      default:
        return "Success!";
    }
  };

  const getMessage = () => {
    if (status === "loading") {
      return "Please wait while we verify your email address...";
    }
    if (status === "error") {
      return errorMessage;
    }

    switch (actionMode) {
      case "verifyEmail":
        return "Your email has been successfully verified. You can now sign in to your account. Redirecting...";
      case "recoverEmail":
        return "Your email has been successfully recovered. Redirecting...";
      default:
        return "Action completed successfully. Redirecting...";
    }
  };

  return (
    <Container size="xs" style={{ paddingTop: "100px" }}>
      <Paper p="xl" shadow="md" radius="md">
        <Stack gap="lg" align="center">
          {status === "loading" && <Loader size="lg" />}

          <div style={{ textAlign: "center" }}>
            <Title order={3} mb="xs">
              {getTitle()}
            </Title>
            <Text size="sm" c={status === "error" ? "red" : "dimmed"}>
              {getMessage()}
            </Text>
          </div>

          {status === "error" && (
            <Button onClick={() => router.push("/login")} fullWidth>
              Return to Login
            </Button>
          )}

          {status === "success" && (
            <Text size="xs" c="dimmed">
              You will be redirected in a few seconds...
            </Text>
          )}
        </Stack>
      </Paper>
    </Container>
  );
};

export default AuthAction;