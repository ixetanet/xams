import React from "react";
import { Button, Title, Text, Stack, Alert } from "@mantine/core";
import { useLoginContext } from "../LoginContext";
import { FirebaseConfig } from "@/types";
import { API_CONFIG } from "@ixeta/xams";
import { useQuery } from "@tanstack/react-query";

const EmailVerificationForm = () => {
  const { auth, loadingStates, setLoadingStates } = useLoginContext();

  return (
    <Stack gap="lg">
      <div>
        <Title order={3} ta="center" mb="xs">
          Verify your email
        </Title>
        <Text size="sm" ta="center" c="dimmed">
          We&apos;ve sent a verification link to your email address. Please
          check your inbox.
        </Text>
      </div>

      {auth.error && (
        <Alert color="red" variant="light">
          {auth.error}
        </Alert>
      )}

      <form
        onSubmit={async (e) => {
          e.preventDefault();
          setLoadingStates((prev) => ({ ...prev, resendEmail: true }));
          try {
            const authRedirectUrl = localStorage.getItem("auth-redirecturl");
            if (authRedirectUrl) {
              await auth.sendEmailVerification({
                url: authRedirectUrl,
              });
            } else {
              await auth.sendEmailVerification();
            }
          } finally {
            setLoadingStates((prev) => ({ ...prev, resendEmail: false }));
          }
        }}
      >
        <Button
          type="submit"
          size="md"
          fullWidth
          loading={loadingStates.resendEmail}
        >
          Resend verification email
        </Button>
      </form>

      <Button
        onClick={() => auth.signOut("login")}
        variant="subtle"
        size="sm"
        fullWidth
        disabled={loadingStates.resendEmail}
      >
        Return to login
      </Button>
    </Stack>
  );
};

export default EmailVerificationForm;
