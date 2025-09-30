import React, { useEffect, useState } from "react";
import {
  Button,
  TextInput,
  Title,
  Text,
  Stack,
  Alert,
  Loader,
  PasswordInput,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

interface ResetPasswordFormProps {
  oobCode: string;
}

export const ResetPasswordForm = ({ oobCode }: ResetPasswordFormProps) => {
  const { auth, loadingStates, setLoadingStates, router } = useLoginContext();
  const [isVerifying, setIsVerifying] = useState(true);
  const [resetSuccess, setResetSuccess] = useState(false);

  useEffect(() => {
    // Set the oobCode in auth state
    auth.setResetOobCode(oobCode);

    // Verify the code is valid and get the email
    const verifyCode = async () => {
      setIsVerifying(true);
      await auth.verifyPasswordResetCode(oobCode);
      setIsVerifying(false);
    };

    verifyCode();
  }, [oobCode]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate passwords match
    if (auth.resetNewPassword !== auth.resetConfirmPassword) {
      auth.setError("Passwords do not match.");
      return;
    }

    // Validate password strength (optional)
    if (auth.resetNewPassword.length < 6) {
      auth.setError("Password must be at least 6 characters long.");
      return;
    }

    setLoadingStates((prev) => ({ ...prev, resetPassword: true }));
    try {
      const success = await auth.confirmPasswordReset(
        oobCode,
        auth.resetNewPassword
      );
      if (success) {
        setResetSuccess(true);
        // Redirect to login after 3 seconds
        setTimeout(() => {
          router.push("/login");
        }, 3000);
      }
    } finally {
      setLoadingStates((prev) => ({ ...prev, resetPassword: false }));
    }
  };

  // Success state
  if (resetSuccess) {
    return (
      <Stack gap="lg">
        <div>
          <Title order={2} ta="center" mb="xs">
            Password Reset Successful!
          </Title>
          <Text size="sm" ta="center" c="dimmed">
            Your password has been successfully reset. You can now sign in with
            your new password.
          </Text>
        </div>

        <Alert color="green" variant="light">
          Redirecting to login page...
        </Alert>
      </Stack>
    );
  }

  // Main form
  return (
    <Stack gap="lg">
      <div>
        <Title order={2} ta="center" mb="xs">
          Reset your password
        </Title>
        <Text size="sm" ta="center" c="dimmed">
          {isVerifying && "Verifying reset link..."}
          {!isVerifying && auth.resetEmailFromCode && (
            <>
              Resetting password for <strong>{auth.resetEmailFromCode}</strong>
            </>
          )}
          {!isVerifying &&
            !auth.resetEmailFromCode &&
            "Enter your new password below."}
        </Text>
      </div>

      {auth.error && (
        <Alert color="red" variant="light">
          {auth.error}
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <PasswordInput
            label="New Password"
            placeholder="Enter your new password"
            value={auth.resetNewPassword}
            onChange={(e) => auth.setResetNewPassword(e.currentTarget.value)}
            size="md"
            disabled={loadingStates.resetPassword}
            required
            autoFocus
          />
          <PasswordInput
            label="Confirm New Password"
            placeholder="Re-enter your new password"
            value={auth.resetConfirmPassword}
            onChange={(e) =>
              auth.setResetConfirmPassword(e.currentTarget.value)
            }
            size="md"
            disabled={loadingStates.resetPassword}
            required
          />
        </Stack>

        <Button
          type="submit"
          size="md"
          mt="md"
          fullWidth
          loading={loadingStates.resetPassword || isVerifying}
          disabled={isVerifying}
        >
          Reset Password
        </Button>
      </form>

      <Text size="sm" ta="center">
        Remember your password?{" "}
        <Text
          component="a"
          c="blue"
          style={{ cursor: "pointer" }}
          onClick={() => router.push("/login")}
        >
          Back to sign in
        </Text>
      </Text>
    </Stack>
  );
};

export default ResetPasswordForm;
