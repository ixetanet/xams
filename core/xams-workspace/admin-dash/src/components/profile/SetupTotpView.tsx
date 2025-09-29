import React from "react";
import {
  Button,
  Paper,
  Title,
  Text,
  Stack,
  Alert,
  Loader,
  Group,
  PinInput,
} from "@mantine/core";
import QRCode from "react-qr-code";
import { useLoginContext } from "../login/LoginContext";

const SetupTotpView = () => {
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
              Setup Authenticator App
            </Title>
            <Text size="sm" ta="center" c="dimmed">
              Scan the QR code with your authenticator app
            </Text>
          </div>

          {auth.error && (
            <Alert color="red" variant="light">
              {auth.error}
            </Alert>
          )}

          {loadingStates.totpCreate && (
            <div className="flex justify-center">
              <Loader size="sm" />
            </div>
          )}

          {auth.mfaTotpQrCode && (
            <div className="flex justify-center">
              <QRCode
                size={256}
                style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                value={auth.mfaTotpQrCode}
                viewBox="0 0 256 256"
              />
            </div>
          )}

          <form
            onSubmit={async (e) => {
              e.preventDefault();
              setLoadingStates((prev) => ({ ...prev, totpEnroll: true }));
              try {
                await auth.mfaTotpEnroll();
                auth.setView("profile");
              } finally {
                setLoadingStates((prev) => ({
                  ...prev,
                  totpEnroll: false,
                }));
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
                    disabled={loadingStates.totpEnroll}
                  />
                </div>
              </div>

              <Group grow>
                <Button
                  type="submit"
                  size="md"
                  loading={loadingStates.totpEnroll}
                >
                  Enroll
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

export default SetupTotpView;
