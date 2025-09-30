import React, { useRef, useEffect } from "react";
import { Button, TextInput, Title, Text, Stack, Alert } from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const RegisterForm = () => {
  const { auth, loadingStates, setLoadingStates, redirectUrls } =
    useLoginContext();
  const emailInputRef = useRef<HTMLInputElement>(null);

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
          Create account
        </Title>
        <Text size="sm" ta="center" c="dimmed">
          Get started by creating your account.
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
          setLoadingStates((prev) => ({ ...prev, signUp: true }));
          try {
            const success = await auth.signUp();
            if (success) {
              const authRedirectUrl = localStorage.getItem("auth-redirecturl");
              if (authRedirectUrl) {
                await auth.sendEmailVerification({
                  url: authRedirectUrl,
                });
              } else {
                await auth.sendEmailVerification();
              }
            }
          } finally {
            setLoadingStates((prev) => ({ ...prev, signUp: false }));
          }
        }}
      >
        <Stack gap="md">
          <TextInput
            ref={emailInputRef}
            label="Email address"
            placeholder="Enter your email"
            value={auth.emailAddress}
            onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
            size="md"
            disabled={loadingStates.signUp}
          />
          <TextInput
            label="Password"
            placeholder="Create a password"
            type="password"
            value={auth.password}
            onChange={(e) => auth.setPassword(e.currentTarget.value)}
            size="md"
            disabled={loadingStates.signUp}
          />
        </Stack>

        <Button
          type="submit"
          size="md"
          mt="md"
          fullWidth
          loading={loadingStates.signUp}
        >
          Create account
        </Button>
      </form>

      <Text size="sm" ta="center">
        Already have an account?{" "}
        <Text
          component="a"
          c="blue"
          style={{ cursor: "pointer" }}
          onClick={() => auth.setView("login")}
        >
          Sign in
        </Text>
      </Text>
    </Stack>
  );
};

export default RegisterForm;
