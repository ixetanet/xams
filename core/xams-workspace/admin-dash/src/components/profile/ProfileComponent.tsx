import React, { useState } from "react";
import {
  Button,
  TextInput,
  Paper,
  Title,
  Text,
  Stack,
  Alert,
  Loader,
  Group,
  Badge,
  Card,
  Divider,
  PinInput,
} from "@mantine/core";
import { useAuth } from "@ixeta/headless-auth-react";
import QRCode from "react-qr-code";
import { useRouter } from "next/router";

const ProfileComponent = () => {
  const auth = useAuth();
  const router = useRouter();
  const [loadingStates, setLoadingStates] = useState({
    totpCreate: false,
    totpEnroll: false,
    smsCreate: false,
    smsEnroll: false,
  });

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  if (!auth.isLoggedIn) {
    router.push("/");
  }

  // Handle MFA setup views
  if (auth.view === "setup_mfa_totp") {
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
                onClick={async () => {
                  setLoadingStates((prev) => ({ ...prev, totpEnroll: true }));
                  try {
                    await auth.mfaTotpEnroll();
                  } finally {
                    setLoadingStates((prev) => ({
                      ...prev,
                      totpEnroll: false,
                    }));
                  }
                }}
                size="md"
                loading={loadingStates.totpEnroll}
              >
                Enroll
              </Button>
              <Button
                variant="outline"
                onClick={() => auth.setView("profile")}
                size="md"
              >
                Cancel
              </Button>
            </Group>
          </Stack>
        </Paper>
      </div>
    );
  }

  if (auth.view === "setup_mfa_sms") {
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

            <TextInput
              label="Phone Number"
              placeholder="+1234567890"
              value={auth.phoneNumber}
              onChange={(e) => auth.setPhoneNumber(e.currentTarget.value)}
              size="md"
              disabled={loadingStates.smsCreate}
            />

            <Group grow>
              <Button
                onClick={async () => {
                  setLoadingStates((prev) => ({ ...prev, smsCreate: true }));
                  try {
                    if (await auth.mfaSmsCreate()) {
                      auth.setView("setup_mfa_sms_enroll");
                    }
                  } finally {
                    setLoadingStates((prev) => ({ ...prev, smsCreate: false }));
                  }
                }}
                size="md"
                loading={loadingStates.smsCreate}
              >
                Send Code
              </Button>
              <Button
                variant="outline"
                onClick={() => auth.setView("profile")}
                size="md"
              >
                Cancel
              </Button>
            </Group>
          </Stack>
        </Paper>
      </div>
    );
  }

  if (auth.view === "setup_mfa_sms_enroll") {
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
                onClick={async () => {
                  setLoadingStates((prev) => ({ ...prev, smsEnroll: true }));
                  try {
                    await auth.mfaSmsEnroll();
                  } finally {
                    setLoadingStates((prev) => ({ ...prev, smsEnroll: false }));
                  }
                }}
                size="md"
                loading={loadingStates.smsEnroll}
              >
                Enroll
              </Button>
              <Button
                variant="outline"
                onClick={() => auth.mfaSmsCreate()}
                size="md"
              >
                Resend Code
              </Button>
            </Group>

            <Button
              variant="subtle"
              onClick={() => auth.setView("profile")}
              size="sm"
              fullWidth
            >
              Cancel
            </Button>
          </Stack>
        </Paper>
      </div>
    );
  }

  // Main profile page
  return (
    <div className="min-h-screen flex justify-center items-center p-4">
      <Paper
        shadow="sm"
        radius="lg"
        p="xl"
        withBorder
        className="w-full max-w-md"
      >
        <Stack gap="lg">
          <Button
            variant="subtle"
            onClick={() => router.back()}
            size="sm"
            style={{ alignSelf: 'flex-start' }}
          >
            ← Back
          </Button>

          <div>
            <Title order={2} ta="center" mb="xs">
              Profile Settings
            </Title>
            <Text size="sm" ta="center" c="dimmed">
              Manage your account security settings
            </Text>
          </div>

          <Divider />

          <div>
            <Title order={4} mb="md">
              Multi-Factor Authentication
            </Title>

            <Stack gap="md">
              <Card withBorder p="md">
                <Stack gap="md">
                  <Group justify="space-between" align="center">
                    <div>
                      <Text fw={500}>Authenticator App</Text>
                      <Text size="sm" c="dimmed">
                        Use an authenticator app for secure login
                      </Text>
                    </div>
                    {auth.mfaTotpEnrolled && (
                      <Badge color="green" variant="light">
                        Enabled
                      </Badge>
                    )}
                  </Group>
                  {!auth.mfaTotpEnrolled && (
                    <Button
                      fullWidth
                      onClick={async () => {
                        setLoadingStates((prev) => ({
                          ...prev,
                          totpCreate: true,
                        }));
                        try {
                          auth.setView("setup_mfa_totp");
                          await auth.mfaTotpCreate();
                        } finally {
                          setLoadingStates((prev) => ({
                            ...prev,
                            totpCreate: false,
                          }));
                        }
                      }}
                      loading={loadingStates.totpCreate}
                    >
                      Setup Authenticator App
                    </Button>
                  )}
                </Stack>
              </Card>

              <Card withBorder p="md">
                <Stack gap="md">
                  <Group justify="space-between" align="center">
                    <div>
                      <Text fw={500}>SMS Authentication</Text>
                      <Text size="sm" c="dimmed">
                        Receive verification codes via SMS
                      </Text>
                    </div>
                    {auth.mfaSmsEnrolled && (
                      <Badge color="green" variant="light">
                        Enabled
                      </Badge>
                    )}
                  </Group>
                  {!auth.mfaSmsEnrolled && (
                    <Button
                      fullWidth
                      onClick={() => auth.setView("setup_mfa_sms")}
                    >
                      Setup SMS Authentication
                    </Button>
                  )}
                </Stack>
              </Card>
            </Stack>

            {!auth.mfaTotpEnrolled && !auth.mfaSmsEnrolled && (
              <Alert color="orange" variant="light" mt="md">
                <Text size="sm">
                  <strong>Security Recommendation:</strong> Enable at least one
                  MFA method to secure your account.
                </Text>
              </Alert>
            )}
          </div>

          <Divider />

          <Button
            variant="outline"
            onClick={() => auth.signOut()}
            size="md"
            fullWidth
          >
            Sign Out
          </Button>
        </Stack>
      </Paper>
    </div>
  );
};

export default ProfileComponent;
