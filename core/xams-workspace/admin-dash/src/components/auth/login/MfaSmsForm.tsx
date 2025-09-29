import React, { useRef, useEffect } from "react";
import {
  Button,
  TextInput,
  Title,
  Text,
  Stack,
  Alert,
  PinInput,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const MfaSmsForm = () => {
  const { auth, loadingStates, setLoadingStates } = useLoginContext();
  const pinInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Focus the first pin input when component mounts
    const timer = setTimeout(() => {
      if (pinInputRef.current) {
        pinInputRef.current.focus();
      }
    }, 100);

    return () => clearTimeout(timer);
  }, []);

  return (
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
              ref={pinInputRef}
              length={6}
              size="md"
              value={auth.mfaCode}
              onChange={(value) => auth.setMfaCode(value)}
              disabled={loadingStates.mfaSms}
              autoFocus
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
  );
};

export default MfaSmsForm;
