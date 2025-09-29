import React from "react";
import { Button, Paper, Title, Stack } from "@mantine/core";
import { useLoginContext } from "../LoginContext";

const MfaSelectionForm = () => {
  const { auth, loadingStates } = useLoginContext();

  return (
    <div className="min-h-screen flex justify-center items-center p-4">
      <Paper
        shadow="sm"
        radius="lg"
        p="xl"
        withBorder
        className="w-full max-w-sm"
      >
        <Stack gap="md">
          <Title order={3} ta="center">
            Choose verification method
          </Title>
          {auth.mfaFactors.map((factor) => {
            if (factor === "totp") {
              return (
                <Button
                  key={factor}
                  onClick={() => auth.setView("mfa_totp")}
                  variant="default"
                  size="md"
                  fullWidth
                  disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
                >
                  Authenticator App
                </Button>
              );
            }
            if (factor === "sms") {
              return (
                <Button
                  key={factor}
                  onClick={() => {
                    auth.setView("mfa_sms");
                    auth.mfaSmsSend();
                  }}
                  variant="default"
                  size="md"
                  fullWidth
                  disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
                >
                  SMS
                </Button>
              );
            }
            return null;
          })}
          <Button
            onClick={() => auth.signOut("login")}
            variant="subtle"
            size="sm"
            mt="xs"
            fullWidth
            disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
          >
            Logout
          </Button>
        </Stack>
      </Paper>
    </div>
  );
};

export default MfaSelectionForm;
