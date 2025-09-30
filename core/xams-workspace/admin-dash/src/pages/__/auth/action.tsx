import React, { useEffect, useState } from "react";
import { useRouter } from "next/router";
import { Title, Text, Stack, Loader, Alert, Button } from "@mantine/core";
import { applyActionCode } from "firebase/auth";
import { firebaseAuth, initializeFirebase } from "../../_app";
import { useQuery } from "@tanstack/react-query";
import { FirebaseConfig } from "@/types";
import { API_CONFIG, getQueryParam } from "@ixeta/xams";
import LoginContainer from "@/components/auth/LoginContainer";

type ActionMode = "verifyEmail" | "resetPassword" | "recoverEmail";

const AuthAction = () => {
  const router = useRouter();
  const [status, setStatus] = useState<"loading" | "success" | "error">(
    "loading"
  );
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [actionMode, setActionMode] = useState<ActionMode | null>(null);
  const [isFirebaseReady, setIsFirebaseReady] = useState(false);

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
    if (
      authQuery.data &&
      Object.keys(authQuery.data).length !== 0 &&
      firebaseAuth === null
    ) {
      console.log("Initializing Firebase with config:", authQuery.data);
      initializeFirebase(authQuery.data);
      setIsFirebaseReady(true);
    } else if (firebaseAuth !== null) {
      console.log("Firebase already initialized");
      setIsFirebaseReady(true);
    }
  }, [authQuery.data]);

  // Add timeout to detect initialization issues
  useEffect(() => {
    const timeout = setTimeout(() => {
      if (status === "loading" && authQuery.data && !isFirebaseReady) {
        console.error("Firebase failed to initialize after 10 seconds");
        setStatus("error");
        setErrorMessage(
          "Failed to initialize authentication. Please try again later."
        );
      }
    }, 10000);

    return () => clearTimeout(timeout);
  }, [status, authQuery.data, isFirebaseReady]);

  useEffect(() => {
    const handleAction = async () => {
      if (!router.isReady || !isFirebaseReady || !firebaseAuth) {
        console.log("Action page waiting:", {
          routerReady: router.isReady,
          firebaseReady: isFirebaseReady,
          firebaseAuth: !!firebaseAuth,
        });
        return;
      }

      const { mode, oobCode, continueUrl } = router.query;
      console.log("Action page processing:", { mode, hasOobCode: !!oobCode });

      if (continueUrl) {
        localStorage.setItem("auth-redirecturl", continueUrl as string);
      }

      if (!mode || !oobCode) {
        setStatus("error");
        setErrorMessage("Invalid verification link. Please try again.");
        return;
      }

      setActionMode(mode as ActionMode);

      try {
        switch (mode) {
          case "verifyEmail":
            console.log("Applying action code for email verification");
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
        console.error("Action page error:", error);
        setStatus("error");

        // Handle specific Firebase error codes
        if (error.code === "auth/invalid-action-code") {
          setErrorMessage("This link has expired or has already been used.");
        } else if (error.code === "auth/expired-action-code") {
          setErrorMessage(
            "This verification link has expired. Please request a new one."
          );
        } else if (error.code === "auth/user-disabled") {
          setErrorMessage("This account has been disabled.");
        } else if (error.code === "auth/user-not-found") {
          setErrorMessage("No account found with this email.");
        } else if (error.code === "auth/network-request-failed") {
          setErrorMessage(
            "Network error. Please check your connection and try again."
          );
        } else {
          setErrorMessage(
            error.message || "An error occurred. Please try again."
          );
        }
      }
    };

    handleAction();
  }, [router.isReady, router.query, isFirebaseReady]);

  // Loading state while fetching Firebase config
  if (authQuery.isLoading) {
    return (
      <LoginContainer>
        <Stack align="center" gap="lg">
          <Loader size="lg" />
          <Text size="sm" c="dimmed">
            Loading...
          </Text>
        </Stack>
      </LoginContainer>
    );
  }

  if (!authQuery.data) {
    return (
      <LoginContainer>
        <Stack gap="lg">
          <Alert color="red" variant="light">
            Error loading authentication settings.
          </Alert>
          <Button onClick={() => router.push("/login")} fullWidth>
            Return to Login
          </Button>
        </Stack>
      </LoginContainer>
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
    <LoginContainer>
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
    </LoginContainer>
  );
};

export default AuthAction;
