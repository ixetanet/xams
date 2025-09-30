import React, { useRef, useEffect } from "react";
import {
  Button,
  TextInput,
  Checkbox,
  Title,
  Text,
  Divider,
  Stack,
  Alert,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const LoginForm = () => {
  const {
    auth,
    providers,
    loadingStates,
    setLoadingStates,
    getProviderDisplayName,
    getProviderIcon,
    isAnyProviderLoading,
  } = useLoginContext();
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
          Sign in
        </Title>
        <Text size="sm" ta="center" c="dimmed">
          Please sign in to continue.
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
          setLoadingStates((prev) => ({ ...prev, signIn: true }));
          try {
            await auth.signIn();
          } finally {
            setLoadingStates((prev) => ({ ...prev, signIn: false }));
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
            disabled={loadingStates.signIn || isAnyProviderLoading()}
          />
          <TextInput
            label="Password"
            placeholder="Enter your password"
            type="password"
            value={auth.password}
            onChange={(e) => auth.setPassword(e.currentTarget.value)}
            size="md"
            disabled={loadingStates.signIn || isAnyProviderLoading()}
          />
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <Checkbox
              label="Remember me"
              checked={auth.remember}
              onChange={(e) => {
                auth.setRemember(e.currentTarget.checked);
              }}
              disabled={loadingStates.signIn || isAnyProviderLoading()}
            />
            <Text
              size="sm"
              c="blue"
              style={{ cursor: "pointer" }}
              onClick={() => auth.setView("forgot_password")}
            >
              Forgot password?
            </Text>
          </div>
        </Stack>

        <Button
          type="submit"
          size="md"
          mt="md"
          fullWidth
          loading={loadingStates.signIn}
        >
          Sign in
        </Button>
      </form>

      {providers.length > 0 && (
        <>
          <Divider label="Or continue with" labelPosition="center" />

          <Stack gap="sm">
            {providers.map((provider) => (
              <Button
                key={provider}
                onClick={async () => {
                  const loadingKey = `${provider}SignIn`;
                  setLoadingStates((prev) => ({
                    ...prev,
                    [loadingKey]: true,
                  }));
                  try {
                    await auth.signInProvider(provider);
                  } finally {
                    setLoadingStates((prev) => ({
                      ...prev,
                      [loadingKey]: false,
                    }));
                  }
                }}
                variant="default"
                size="md"
                fullWidth
                disabled={loadingStates.signIn || isAnyProviderLoading()}
                loading={loadingStates[`${provider}SignIn`] || false}
                leftSection={getProviderIcon(provider)}
              >
                Continue with {getProviderDisplayName(provider)}
              </Button>
            ))}
          </Stack>
        </>
      )}

      <Text size="sm" ta="center">
        Don&apos;t have an account?{" "}
        <Text
          component="a"
          c="blue"
          style={{ cursor: "pointer" }}
          onClick={() => auth.setView("register")}
        >
          Sign up
        </Text>
      </Text>
    </Stack>
  );
};

export default LoginForm;
