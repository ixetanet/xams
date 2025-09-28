import React from "react";
import { Button, Paper, Title, Text, Stack, Alert } from "@mantine/core";
import { sendEmailVerification } from "firebase/auth";
import { firebaseAuth } from "@/pages/_app";
import { useLoginContext } from "./LoginContext";

const EmailVerificationForm = () => {
  const { auth, loadingStates, setLoadingStates } = useLoginContext();

  return (
    <div className="min-h-screen flex justify-center items-center p-4">
      <Paper
        shadow="sm"
        radius="lg"
        p="xl"
        withBorder
        className="w-full max-w-sm"
      >
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
                if (firebaseAuth == null) {
                  return;
                }
                if (firebaseAuth.currentUser === null) {
                  return;
                }
                await sendEmailVerification(firebaseAuth.currentUser);
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
        </Stack>
      </Paper>
    </div>
  );
};

export default EmailVerificationForm;
