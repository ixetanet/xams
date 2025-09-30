import React, { useRef, useEffect } from "react";
import {
  Button,
  TextInput,
  Title,
  Text,
  Stack,
  Alert,
  Group,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const SetupSmsView = () => {
  const { auth, loadingStates, setLoadingStates } = useLoginContext();
  const phoneInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Focus the phone input when component mounts
    const timer = setTimeout(() => {
      if (phoneInputRef.current) {
        phoneInputRef.current.focus();
      }
    }, 100);

    return () => clearTimeout(timer);
  }, []);

  return (
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
                ref={phoneInputRef}
                label="Phone Number"
                placeholder="+1234567890"
                value={auth.phoneNumber}
                onChange={(e) => auth.setPhoneNumber(e.currentTarget.value)}
                size="md"
                disabled={loadingStates.smsCreate}
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
  );
};

export default SetupSmsView;
