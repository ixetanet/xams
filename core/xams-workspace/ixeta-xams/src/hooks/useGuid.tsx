import React from "react";
import { v4 as uuidv4, validate as uuidValidate } from "uuid";

const useGuid = () => {
  const getGuid = () => {
    return uuidv4();
  };

  const validate = (str: string) => {
    return uuidValidate(str);
  };
  return {
    get: getGuid,
    validate: validate,
  };
};

export default useGuid;
