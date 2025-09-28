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
            <TextInput
              label="SMS code"
              placeholder="000000"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
              size="md"
              disabled={loadingStates.mfaSms}
            />
            <Button
              type="submit"
              size="md"
              fullWidth
              loading={loadingStates.mfaSms}
            >
              Verify
            </Button>
          </Stack>
        </form>
      </Paper>
    </div>
  );
};

export default MfaSmsForm;
