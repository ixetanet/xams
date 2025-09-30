import React from "react";
import { Paper } from "@mantine/core";

interface LoginContainerProps {
  children: React.ReactNode;
  maxWidth?: "sm" | "md";
}

export const LoginContainer = ({
  children,
  maxWidth = "sm",
}: LoginContainerProps) => {
  const maxWidthClass = maxWidth === "md" ? "max-w-md" : "max-w-sm";

  return (
    <div className="min-h-screen flex justify-center items-center p-4">
      <Paper
        shadow="sm"
        radius="lg"
        p="xl"
        withBorder
        className={`w-full ${maxWidthClass}`}
      >
        {children}
      </Paper>
    </div>
  );
};

export default LoginContainer;
