import React from "react";
import { LogMessage } from "./LogsViewer";
import { Grid, Text } from "@mantine/core";
import { toLocalTimeStringWithAMPM } from "../../../utils/Util";

interface LogItemProps {
  log: LogMessage;
}

const LogItem = ({ log }: LogItemProps) => {
  const [expanded, setExpanded] = React.useState(false);
  return (
    <>
      <span
        className="w-full flex justify-start cursor-pointer items-start"
        onClick={() => setExpanded((x) => !x)}
      >
        <div className="w-60 flex-shrink-0 flex justify-between items-center">
          <Text size="sm">{toLocalTimeStringWithAMPM(log.Timestamp)}</Text>
          {log.Level == "Warning" && (
            <div className=" bg-yellow-500 rounded-full h-3 w-3 mr-4"></div>
          )}
          {log.Level == "Error" && (
            <div className="bg-red-500 rounded-full h-3 w-3 mr-4"></div>
          )}
          {log.Level == "Critical" && (
            <div className="flex justify-center items-center mr-4 text-red-500">
              <div className="bg-red-500 animate-pulse rounded-full h-3 w-3"></div>
              !
            </div>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <Text size="sm" className="break-words whitespace-pre-wrap">
            {log.Message}
          </Text>
        </div>
      </span>
      {expanded && (
        <div className="w-full">
          <div className="w-full flex justify-start my-1">
            <div className="w-60 flex-shrink-0"></div>
            {log.Properties && (
              <div className="flex-1 min-w-0">
                {Object.entries(log.Properties).map(([key, value]) => (
                  <div key={key}>
                    <Text size="sm" className="break-words">
                      <strong>{key}:</strong> {JSON.stringify(value)}
                    </Text>
                  </div>
                ))}
              </div>
            )}
          </div>
          {(log.Exception || log.StackTrace) && (
            <>
              <div className="w-full flex justify-start my-1">
                <div className="w-60 flex-shrink-0"></div>
                <Text size="sm" className="break-words whitespace-pre-wrap">
                  {log.Exception}
                </Text>
              </div>
            </>
          )}
        </div>
      )}
    </>
  );
};

export default LogItem;
