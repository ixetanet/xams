import React, { useState } from "react";
import {
  Modal,
  Text,
  Button,
  Stack,
  Alert,
  PasswordInput,
} from "@mantine/core";
import { useLoginContext } from "../LoginContext";

interface ChangePasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
  loadingStates: Record<string, boolean>;
  setLoadingStates: React.Dispatch<
    React.SetStateAction<Record<string, boolean>>
  >;
}

const ChangePasswordModal = ({
  isOpen,
  onClose,
  loadingStates,
  setLoadingStates,
}: ChangePasswordModalProps) => {
  const { auth } = useLoginContext();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState(false);

  const handleClose = () => {
    // Wait for aniomation to finish
    setTimeout(() => {
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setValidationError(null);
      setSuccessMessage(false);
    }, 500);
    onClose();
  };

  const validatePasswords = (): boolean => {
    if (!currentPassword) {
      setValidationError("Current password is required");
      return false;
    }
    if (!newPassword) {
      setValidationError("New password is required");
      return false;
    }
    if (newPassword.length < 6) {
      setValidationError("New password must be at least 6 characters");
      return false;
    }
    if (newPassword !== confirmPassword) {
      setValidationError("New passwords do not match");
      return false;
    }
    if (currentPassword === newPassword) {
      setValidationError(
        "New password must be different from current password"
      );
      return false;
    }
    setValidationError(null);
    return true;
  };

  const handleChangePassword = async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault();
    }

    if (!validatePasswords()) {
      return;
    }

    setLoadingStates((prev) => ({
      ...prev,
      changePassword: true,
    }));

    try {
      // Pass the passwords directly to avoid state timing issues
      const success = await auth.changePassword(currentPassword, newPassword);

      if (success) {
        setSuccessMessage(true);
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
        setTimeout(() => {
          handleClose();
        }, 2000);
      }
    } finally {
      setLoadingStates((prev) => ({
        ...prev,
        changePassword: false,
      }));
    }
  };

  return (
    <Modal
      opened={isOpen}
      onClose={handleClose}
      title="Change Password"
      centered
      size="md"
    >
      <form onSubmit={handleChangePassword}>
        <Stack gap="lg">
          {successMessage && (
            <Alert color="green" variant="light">
              <Text size="sm">Password changed successfully!</Text>
            </Alert>
          )}

          {!successMessage && (
            <>
              {validationError && (
                <Alert color="red" variant="light">
                  <Text size="sm">{validationError}</Text>
                </Alert>
              )}

              {auth.error && (
                <Alert color="red" variant="light">
                  <Text size="sm">{auth.error}</Text>
                </Alert>
              )}

              <PasswordInput
                label="Current Password"
                placeholder="Enter your current password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.currentTarget.value)}
                required
                disabled={loadingStates.changePassword}
                data-autofocus
              />

              <PasswordInput
                label="New Password"
                placeholder="Enter your new password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.currentTarget.value)}
                required
                disabled={loadingStates.changePassword}
              />

              <PasswordInput
                label="Confirm New Password"
                placeholder="Confirm your new password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.currentTarget.value)}
                required
                disabled={loadingStates.changePassword}
              />

              <div className="grid grid-cols-2 gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleClose}
                  disabled={loadingStates.changePassword}
                  fullWidth
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  loading={loadingStates.changePassword}
                  fullWidth
                >
                  Change Password
                </Button>
              </div>
            </>
          )}
        </Stack>
      </form>
    </Modal>
  );
};

export default ChangePasswordModal;
