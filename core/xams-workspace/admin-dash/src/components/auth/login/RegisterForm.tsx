import React from "react";
import {
  Button,
  TextInput,
  Paper,
  Title,
  Text,
  Stack,
  Alert,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const RegisterForm = () => {
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
                await auth.signUp();
              } finally {
                setLoadingStates((prev) => ({ ...prev, signUp: false }));
              }
            }}
          >
            <Stack gap="md">
              <TextInput
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
      </Paper>
    </div>
  );
};

export default RegisterForm;
