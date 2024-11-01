import { CopyButton, Button } from "@mantine/core";
import { IconCheck, IconClipboard } from "@tabler/icons-react";
import React from "react";

interface CopyProps {
  value: string;
}

const CopyId = (props: CopyProps) => {
  return (
    <>
      <span>Id: {props.value}</span>
      <CopyButton value={props.value}>
        {({ copied, copy }) => (
          <Button
            variant="subtle"
            styles={{
              root: {
                paddingRight: 4,
                paddingLeft: 4,
              },
            }}
            onClick={copy}
          >
            {copied ? (
              <IconCheck size={16} />
            ) : (
              <IconClipboard className="mr-1" size={16} />
            )}

            {copied ? "Copied" : "Copy"}
          </Button>
        )}
      </CopyButton>
    </>
  );
};

export default CopyId;
