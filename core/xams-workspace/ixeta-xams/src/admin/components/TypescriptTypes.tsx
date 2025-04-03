import { Textarea } from "@mantine/core";
import React, { useEffect, useState } from "react";
import useAuthRequest from "../../hooks/useAuthRequest";

const TypescriptTypes = () => {
  const authRequest = useAuthRequest();
  const [value, setValue] = useState("");

  const getTypes = async () => {
    const resp = await authRequest.action("ADMIN_GetTypes");
    if (resp.succeeded) {
      setValue(resp.data as string);
    }
  };

  useEffect(() => {
    getTypes();
  }, []);

  return (
    <Textarea
      styles={{
        root: {
          width: "100%",
          height: "100%",
        },
        input: {
          width: "100%",
          height: "100%",
        },
        wrapper: {
          width: "100%",
          height: "100%",
        },
      }}
      readOnly
      value={value}
    ></Textarea>
  );
};

export default TypescriptTypes;
