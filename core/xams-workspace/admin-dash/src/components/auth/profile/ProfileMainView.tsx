import React from "react";
import {
  Button,
  Title,
  Text,
  Stack,
  Alert,
  Group,
  Badge,
  Card,
  Divider,
} from "@mantine/core";
import DeactivateMfaModal from "./DeactivateMfaModal";
import { useLoginContext } from "../LoginContext";

const ProfileMainView = () => {
  const {
    auth,
    router,
    loadingStates,
    setLoadingStates,
    deactivateModal,
    setDeactivateModal,
  } = useLoginContext();

  return (
    <>
      <Stack gap="lg">
        <Button
          variant="subtle"
          onClick={() => router.back()}
          size="sm"
          style={{ alignSelf: "flex-start" }}
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

          {auth.isReLoginRequired && (
            <Alert color="blue" variant="light" mb="md">
              <Text size="sm">
                <strong>Re-authentication Required:</strong> For security
                reasons, you need to sign in again before deactivating MFA.
                Please log in with your current credentials.
              </Text>
            </Alert>
          )}

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
                {!auth.mfaTotpEnrolled ? (
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
                ) : (
                  <Button
                    fullWidth
                    variant="outline"
                    color="red"
                    onClick={() => {
                      setDeactivateModal({
                        isOpen: true,
                        type: "totp",
                      });
                    }}
                    loading={loadingStates.totpUnenroll}
                  >
                    Deactivate Authenticator App
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
                {!auth.mfaSmsEnrolled ? (
                  <Button
                    fullWidth
                    onClick={() => auth.setView("setup_mfa_sms")}
                  >
                    Setup SMS Authentication
                  </Button>
                ) : (
                  <Button
                    fullWidth
                    variant="outline"
                    color="red"
                    onClick={() => {
                      setDeactivateModal({
                        isOpen: true,
                        type: "sms",
                      });
                    }}
                    loading={loadingStates.smsUnenroll}
                  >
                    Deactivate SMS Authentication
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
          onClick={() => auth.signOut("login")}
          size="md"
          fullWidth
        >
          Sign Out
        </Button>
      </Stack>

      <DeactivateMfaModal
        isOpen={deactivateModal.isOpen}
        type={deactivateModal.type}
        onClose={() => setDeactivateModal({ isOpen: false, type: null })}
        loadingStates={loadingStates}
        setLoadingStates={setLoadingStates}
      />
    </>
  );
};

export default ProfileMainView;
