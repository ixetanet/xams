import React from "react";
import {
  Button,
  TextInput,
  Paper,
  Title,
  Text,
  Stack,
  Alert,
  Group,
} from "@mantine/core";
import { useLoginContext } from "../login/LoginContext";

const SetupSmsView = () => {
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
              Setup SMS MFA
            </Title>
            <Text size="sm" ta="center" c="dimmed">
              Enter your phone number to receive verification codes
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
              setLoadingStates((prev) => ({ ...prev, smsCreate: true }));
              try {
                if (await auth.mfaSmsCreate()) {
                  auth.setView("setup_mfa_sms_enroll");
                }
              } finally {
                setLoadingStates((prev) => ({ ...prev, smsCreate: false }));
              }
            }}
          >
            <Stack gap="lg">
              <TextInput
                label="Phone Number"
                placeholder="+1234567890"
                value={auth.phoneNumber}
                onChange={(e) => auth.setPhoneNumber(e.currentTarget.value)}
                size="md"
                disabled={loadingStates.smsCreate}
                autoFocus
              />

              <Group grow>
                <Button
                  type="submit"
                  size="md"
                  loading={loadingStates.smsCreate}
                >
                  Send Code
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => auth.setView("profile")}
                  size="md"
                >
                  Cancel
                </Button>
              </Group>
            </Stack>
          </form>
        </Stack>
      </Paper>
    </div>
  );
};

export default SetupSmsView;
