import { Button, Drawer } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import React from "react";
import Highlighter from "./Highlighter";

interface CodeExampleProps {
  example: JSX.Element;
}

const CodeExample = (props: CodeExampleProps) => {
  const [opened, handlers] = useDisclosure(false);
  return (
    <>
      <Button variant="light" onClick={handlers.open}>
        View Code
      </Button>
      <Drawer
        position="right"
        size="xl"
        opened={opened}
        onClose={handlers.close}
      >
        {props.example}
      </Drawer>
    </>
  );
};

export default CodeExample;
