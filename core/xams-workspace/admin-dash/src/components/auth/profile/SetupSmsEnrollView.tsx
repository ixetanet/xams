import React from "react";
import {
  Button,
  Title,
  Text,
  Stack,
  Alert,
  Group,
  PinInput,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const SetupSmsEnrollView = () => {
  const { auth, loadingStates, setLoadingStates } = useLoginContext();

  return (
    <Stack gap="lg">
          <div>
            <Title order={3} ta="center" mb="xs">
              Verify SMS Code
            </Title>
            <Text size="sm" ta="center" c="dimmed">
              Enter the verification code sent to your phone
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
              setLoadingStates((prev) => ({ ...prev, smsEnroll: true }));
              try {
                await auth.mfaSmsEnroll();
                auth.setView("profile");
              } finally {
                setLoadingStates((prev) => ({ ...prev, smsEnroll: false }));
              }
            }}
          >
            <Stack gap="lg">
              <div>
                <Text size="sm" fw={500} mb="xs">
                  Verification Code
                </Text>
                <div className="flex justify-center">
                  <PinInput
                    length={6}
                    size="md"
                    value={auth.mfaCode}
                    onChange={(value) => auth.setMfaCode(value)}
                    disabled={loadingStates.smsEnroll}
                  />
                </div>
              </div>

              <Group grow>
                <Button
                  type="submit"
                  size="md"
                  loading={loadingStates.smsEnroll}
                >
                  Enroll
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => auth.mfaSmsCreate()}
                  size="md"
                >
                  Resend Code
                </Button>
              </Group>

              <Button
                type="button"
                variant="subtle"
                onClick={() => auth.setView("profile")}
                size="sm"
                fullWidth
              >
                Cancel
              </Button>

              <Button
                onClick={() => auth.signOut("login")}
                variant="subtle"
                size="sm"
                mt="xs"
                fullWidth
              >
                Logout
              </Button>
            </Stack>
          </form>
    </Stack>
  );
};

export default SetupSmsEnrollView;
