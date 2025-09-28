import React from "react";
import {
  Modal,
  Text,
  Button,
  Group,
  Stack,
  Alert,
} from "@mantine/core";
import { useAuth } from "@ixeta/headless-auth-react";

interface DeactivateMfaModalProps {
  isOpen: boolean;
  type: 'totp' | 'sms' | null;
  onClose: () => void;
  loadingStates: {
    totpUnenroll: boolean;
    smsUnenroll: boolean;
  };
  setLoadingStates: React.Dispatch<React.SetStateAction<{
    totpCreate: boolean;
    totpEnroll: boolean;
    totpUnenroll: boolean;
    smsCreate: boolean;
    smsEnroll: boolean;
    smsUnenroll: boolean;
  }>>;
}

const DeactivateMfaModal = ({
  isOpen,
  type,
  onClose,
  loadingStates,
  setLoadingStates,
}: DeactivateMfaModalProps) => {
  const auth = useAuth();

  const handleDeactivate = async () => {
    if (type === 'totp') {
      setLoadingStates((prev) => ({
        ...prev,
        totpUnenroll: true,
      }));
      try {
        const success = await auth.mfaTotpUnenroll();
        if (success) {
          onClose();
        }
      } finally {
        setLoadingStates((prev) => ({
          ...prev,
          totpUnenroll: false,
        }));
      }
    } else if (type === 'sms') {
      setLoadingStates((prev) => ({
        ...prev,
        smsUnenroll: true,
      }));
      try {
        const success = await auth.mfaSmsUnenroll();
        if (success) {
          onClose();
        }
      } finally {
        setLoadingStates((prev) => ({
          ...prev,
          smsUnenroll: false,
        }));
      }
    }
  };

  const getTitle = () => {
    if (type === 'totp') return 'Deactivate Authenticator App';
    if (type === 'sms') return 'Deactivate SMS Authentication';
    return 'Deactivate MFA';
  };

  const getMessage = () => {
    if (type === 'totp') {
      return 'Are you sure you want to deactivate the Authenticator App? This will reduce your account security.';
    }
    if (type === 'sms') {
      return 'Are you sure you want to deactivate SMS Authentication? This will reduce your account security.';
    }
    return 'Are you sure you want to deactivate this MFA method?';
  };

  const isLoading = type === 'totp' ? loadingStates.totpUnenroll : loadingStates.smsUnenroll;

  return (
    <Modal
      opened={isOpen}
      onClose={onClose}
      title={getTitle()}
      centered
      size="md"
    >
      <Stack gap="lg">
        <Alert color="orange" variant="light">
          <Text size="sm">
            {getMessage()}
          </Text>
        </Alert>

        <Text size="sm" c="dimmed">
          You can always re-enable this authentication method later from your profile settings.
        </Text>

        <Group justify="flex-end" gap="sm">
          <Button
            variant="outline"
            onClick={onClose}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button
            color="red"
            onClick={handleDeactivate}
            loading={isLoading}
          >
            Deactivate
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

export default DeactivateMfaModal;