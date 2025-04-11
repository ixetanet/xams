import React, { useEffect, useRef, useState } from "react";
import { useFormBuilderType } from "../hooks/useFormBuilder";
import useAuthRequest from "../hooks/useAuthRequest";
import { Button } from "@mantine/core";

interface JobFormProps {
  formBuilder: useFormBuilderType;
}

const JobForm = (props: JobFormProps) => {
  const authRequest = useAuthRequest();
  const dataRef = useRef(null);
  const [loading, setIsLoading] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const onTriggerJob = async () => {
    setIsLoading(true);
    props.formBuilder.dispatch({
      type: "SET_FIELD_VALUE",
      payload: { field: "Status", value: "Running" },
    });
    await authRequest.action("ADMIN_TriggerJob", {
      jobName: props.formBuilder.snapshot?.Name,
      // parameters: {
      //   JobId: props.formBuilder.snapshot?.JobId,
      //   JobName: props.formBuilder.snapshot?.Name,
      // },
    });
    setIsLoading(false);
  };

  // useEffect(() => {
  //   const refresh = async () => {
  //     if (dataRef.current == null) {
  //       return;
  //     }
  //     if (isRefreshing) {
  //       return;
  //     }
  //     setIsRefreshing(true);
  //     try {
  //       const resp = await authRequest.read<any>({
  //         tableName: props.formBuilder.tableName,
  //         id: props.formBuilder.snapshot?.JobId,
  //         fields: ["*"],
  //       });
  //       if (resp != null && resp.data.results.length > 0) {
  //         const job = resp.data.results[0];
  //         if (job.Status != "Running") {
  //           setIsLoading(false);
  //         } else {
  //           setIsLoading(true);
  //         }

  //         // If the status has changed, reload the history table
  //         props.formBuilder.dispatch({
  //           type: "SET_FIELD_VALUE",
  //           payload: { field: "Status", value: job.Status },
  //         });
  //         props.formBuilder.dispatch({
  //           type: "SET_FIELD_VALUE",
  //           payload: { field: "LastExecution", value: job.LastExecution },
  //         });
  //       }
  //     } catch (e) {
  //       console.error(e);
  //     } finally {
  //       setIsRefreshing(false);
  //     }
  //   };
  //   const interval = setInterval(refresh, 2500);

  //   return () => clearInterval(interval);
  // }, []);

  // useEffect(() => {
  //   if (
  //     props.formBuilder.snapshot != null &&
  //     props.formBuilder.snapshot?.Status === "Running"
  //   ) {
  //     setIsLoading(true);
  //   }

  //   if (props.formBuilder.snapshot != null) {
  //     dataRef.current = props.formBuilder.snapshot;
  //   }
  // }, [props.formBuilder.snapshot]);

  if (props.formBuilder.snapshot == null) {
    return <></>;
  }

  if (
    props.formBuilder.snapshot?.Name == null ||
    props.formBuilder.snapshot?.name === ""
  ) {
    return <></>;
  }

  if (props.formBuilder.snapshot?._ui_info_ == null) {
    return <></>;
  }

  if (props.formBuilder.snapshot?._ui_info_.canTrigger === true) {
    return (
      <Button variant="outline" onClick={onTriggerJob} loading={loading}>
        Run Job
      </Button>
    );
  }
  return <></>;
};

export default JobForm;
