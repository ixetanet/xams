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

const MfaTotpForm = () => {
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
        setLoadingStates((prev) => ({ ...prev, mfaTotp: true }));
        try {
          await auth.mfaTotpVerify();
        } finally {
          setLoadingStates((prev) => ({ ...prev, mfaTotp: false }));
        }
      }}
    >
      <Stack gap="lg">
        <div>
          <Title order={3} ta="center" mb="xs">
            Enter verification code
          </Title>
          <Text size="sm" ta="center" c="dimmed">
            Enter the code from your authenticator app
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
              ref={pinInputRef}
              length={6}
              size="md"
              value={auth.mfaCode}
              onChange={(value) => auth.setMfaCode(value)}
              disabled={loadingStates.mfaTotp}
              autoFocus
            />
          </div>
        </div>
        <div className="w-full flex flex-col gap-1">
          <Button
            type="submit"
            size="md"
            fullWidth
            loading={loadingStates.mfaTotp}
          >
            Verify
          </Button>
          <Button
            onClick={() => auth.signOut("login")}
            variant="subtle"
            size="sm"
            mt="xs"
            fullWidth
            disabled={loadingStates.mfaTotp}
          >
            Logout
          </Button>
        </div>
      </Stack>
    </form>
  );
};

export default MfaTotpForm;
