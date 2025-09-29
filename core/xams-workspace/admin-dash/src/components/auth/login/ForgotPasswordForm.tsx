import React, { useRef, useEffect, useState } from "react";
import {
  Button,
  TextInput,
  Title,
  Text,
  Stack,
  Alert,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const ForgotPasswordForm = () => {
  const {
    auth,
    loadingStates,
    setLoadingStates,
  } = useLoginContext();
  const emailInputRef = useRef<HTMLInputElement>(null);
  const [emailSent, setEmailSent] = useState(false);

  useEffect(() => {
    // Focus the email input when component mounts
    const timer = setTimeout(() => {
      if (emailInputRef.current) {
        emailInputRef.current.focus();
      }
    }, 100);

    return () => clearTimeout(timer);
  }, []);

  return (
    <Stack gap="lg">
      <div>
        <Title order={2} ta="center" mb="xs">
          Reset password
        </Title>
        <Text size="sm" ta="center" c="dimmed">
          {emailSent
            ? "Check your email for a password reset link."
            : "Enter your email address and we'll send you a link to reset your password."}
        </Text>
      </div>

      {auth.error && (
        <Alert color="red" variant="light">
          {auth.error}
        </Alert>
      )}

      {emailSent && (
        <Alert color="green" variant="light">
          Password reset email sent! Check your inbox and spam folder.
        </Alert>
      )}

      {!emailSent && (
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            setLoadingStates((prev) => ({ ...prev, forgotPassword: true }));
            try {
              const success = await auth.sendPasswordResetEmail();
              if (success) {
                setEmailSent(true);
              }
            } finally {
              setLoadingStates((prev) => ({ ...prev, forgotPassword: false }));
            }
          }}
        >
          <Stack gap="md">
            <TextInput
              ref={emailInputRef}
              label="Email address"
              placeholder="Enter your email"
              value={auth.resetEmail}
              onChange={(e) => auth.setResetEmail(e.currentTarget.value)}
              size="md"
              disabled={loadingStates.forgotPassword}
              required
            />
          </Stack>

          <Button
            type="submit"
            size="md"
            mt="md"
            fullWidth
            loading={loadingStates.forgotPassword}
          >
            Send reset link
          </Button>
        </form>
      )}

      <Text size="sm" ta="center">
        Remember your password?{" "}
        <Text
          component="a"
          c="blue"
          style={{ cursor: "pointer" }}
          onClick={() => {
            setEmailSent(false);
            auth.setView("login");
          }}
        >
          Back to sign in
        </Text>
      </Text>
    </Stack>
  );
};

export default ForgotPasswordForm;