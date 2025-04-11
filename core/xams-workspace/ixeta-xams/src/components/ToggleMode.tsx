import {
  ActionIcon,
  useMantineColorScheme,
  useMantineTheme,
} from "@mantine/core";
import { IconMoonStars, IconSun } from "@tabler/icons-react";
import React, { useEffect } from "react";

interface ToggleModeProps {
  darkColor?: string;
  lightColor?: string;
}

const ToggleMode = (props: ToggleModeProps) => {
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const [mounted, setMounted] = React.useState(false);

  // This prevents hydration error on server-side rendering
  useEffect(() => {
    setMounted(true);
  }, []);

  if (colorScheme === "dark") {
    document.documentElement.classList.add("dark");
  } else {
    document.documentElement.classList.remove("dark");
  }

  if (!mounted) {
    return null;
  }

  const dark = colorScheme === "dark";
  let color = "yellow";
  if (dark && props.darkColor) {
    color = props.darkColor;
  }
  if (!dark && props.lightColor) {
    color = props.lightColor;
  }

  return (
    <ActionIcon
      variant="outline"
      color={color}
      onClick={() => toggleColorScheme()}
      title="Toggle color scheme"
    >
      {dark ? <IconSun size="1.1rem" /> : <IconMoonStars size="1.1rem" />}
    </ActionIcon>
  );
};

export default ToggleMode;
