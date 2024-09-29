import { useMantineColorScheme, useMantineTheme } from "@mantine/core";
import React from "react";

const useColor = () => {
  const theme = useMantineTheme();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const dark = colorScheme === "dark";
  const color = dark ? theme.colors.gray[4] : theme.colors.dark[7];
  return {
    getIconColor: () => color,
    getPrimaryColor: () => theme.fn.primaryColor(),
    colorScheme: colorScheme as "light" | "dark",
  };
};

export default useColor;
