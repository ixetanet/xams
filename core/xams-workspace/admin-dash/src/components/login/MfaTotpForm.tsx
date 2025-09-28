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
import { useLoginContext } from "./LoginContext";

const MfaTotpForm = () => {
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
            <TextInput
              label="Verification code"
              placeholder="000000"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
              size="md"
              disabled={loadingStates.mfaTotp}
            />
            <Button
              type="submit"
              size="md"
              fullWidth
              loading={loadingStates.mfaTotp}
            >
              Verify
            </Button>
          </Stack>
        </form>
      </Paper>
    </div>
  );
};

export default MfaTotpForm;
