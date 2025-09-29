import React from "react";
import {
  Button,
  TextInput,
  Paper,
  Title,
  Text,
  Stack,
  Alert,
  PinInput,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const MfaSmsForm = () => {
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
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            setLoadingStates((prev) => ({ ...prev, mfaSms: true }));
            try {
              await auth.mfaSmsVerify();
            } finally {
              setLoadingStates((prev) => ({ ...prev, mfaSms: false }));
            }
          }}
        >
          <Stack gap="lg">
            <div>
              <Title order={3} ta="center" mb="xs">
                Enter SMS code
              </Title>
              <Text size="sm" ta="center" c="dimmed">
                Enter the code sent to your phone
              </Text>
            </div>
            {auth.error && (
              <Alert color="red" variant="light">
                {auth.error}
              </Alert>
            )}
            <div>
              <Text size="sm" fw={500} mb="xs">
                SMS Code
              </Text>
              <div className="flex justify-center">
                <PinInput
                  length={6}
                  size="md"
                  value={auth.mfaCode}
                  onChange={(value) => auth.setMfaCode(value)}
                  disabled={loadingStates.mfaSms}
                />
              </div>
            </div>
            <Button
              type="submit"
              size="md"
              fullWidth
              loading={loadingStates.mfaSms}
            >
              Verify
            </Button>
            <Button
              onClick={() => auth.signOut("login")}
              variant="subtle"
              size="sm"
              mt="xs"
              fullWidth
              disabled={loadingStates.mfaSms}
            >
              Logout
            </Button>
          </Stack>
        </form>
      </Paper>
    </div>
  );
};

export default MfaSmsForm;
