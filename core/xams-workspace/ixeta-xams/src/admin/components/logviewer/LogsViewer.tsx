import {
  Box,
  Button,
  Checkbox,
  Grid,
  Loader,
  Paper,
  TextInput,
  Tooltip,
} from "@mantine/core";
import React, { useEffect, useState } from "react";
import { useAppContext } from "../../../contexts/AppContext";
import { HubConnectionState } from "@microsoft/signalr";
import LogItem from "./LogItem";
import {
  IconPlayerPlayFilled,
  IconActivity,
  IconPlayerStopFilled,
} from "@tabler/icons-react";
import { DatePickerInput } from "@mantine/dates";

export type LogLevelString =
  | "Trace"
  | "Debug"
  | "Information"
  | "Warning"
  | "Error"
  | "Critical"
  | "None";

export interface LogMessage {
  LogId: string;
  Message: string;
  Timestamp: string;
  Level: LogLevelString;
  SourceContext: string;
  Properties: any;
  Exception?: string;
  StackTrace?: string;
}

interface LogViewerProps {
  isLive?: boolean;
  jobHistoryId?: string;
}

const LogsViewer = (props: LogViewerProps) => {
  const appContext = useAppContext();
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const [isLoading, setIsLoading] = useState(true);
  const [logs, setLogs] = useState<LogMessage[]>([]);
  const [isLive, setIsLive] = useState(props.isLive ?? true);
  const [search, setSearch] = useState<string>("");
  const [userId, setUserId] = useState<string>("");
  const [dateRange, setDateRange] = useState<[string | null, string | null]>([
    null,
    null,
  ]);

  type LogLevelState = {
    all: boolean;
    Critical: boolean;
    Error: boolean;
    Warning: boolean;
    Information: boolean;
    Debug: boolean;
  };

  const [logLevels, setLogLevels] = useState<LogLevelState>({
    all: true,
    Critical: false,
    Error: false,
    Warning: false,
    Information: false,
    Debug: false,
  });

  const getData = async (levels = logLevels) => {
    setIsLoading(true);
    setLogs([]);
    const signalR = await appContext.signalR();
    const selectedLevels: LogLevelString[] = levels.all
      ? ["Critical", "Error", "Warning", "Information", "Debug"]
      : (Object.entries(levels)
          .filter(([key, value]) => value && key !== "all")
          .map(([key]) => key) as LogLevelString[]);

    const data = await signalR.invoke(
      "Logging",
      JSON.stringify({
        Type: "initial_load",
        Message: "",
        Levels: selectedLevels,
        Search: search,
        UserId: userId,
        StartDate: dateRange[0],
        EndDate: dateRange[1],
        JobHistoryId: props.jobHistoryId,
      })
    );
    setLogs(data ?? []);
    setIsLoading(false);
  };

  const handleCheckboxChange = (
    checkbox: keyof LogLevelState,
    checked: boolean
  ) => {
    let newLevels = { ...logLevels };

    if (checkbox === "all") {
      // If "All" is checked, uncheck all others
      newLevels = {
        all: checked,
        Critical: false,
        Error: false,
        Warning: false,
        Information: false,
        Debug: false,
      };
    } else {
      // If any other checkbox is checked, uncheck "All"
      newLevels = {
        ...newLevels,
        all: false,
        [checkbox]: checked,
      };
    }

    setLogLevels(newLevels);
    getData(newLevels);
  };

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    getData();
  };

  useEffect(() => {
    let cleanup = undefined as undefined | (() => void);
    const initialize = async () => {
      if (!isLive && isInitialLoad) {
        await getData();
      }
      if (isLive) {
        await getData();
        const signalR = await appContext.signalR();
        signalR.on("Log", (message: LogMessage) => {
          setLogs((prevLogs) => [message, ...prevLogs].slice(0, 250));
        });
        cleanup = () => signalR.off("Log");
      }
      setIsInitialLoad(false);
    };
    if (appContext.signalRState === HubConnectionState.Connected) {
      initialize();
    }
    return () => {
      cleanup?.();
    };
  }, [appContext.signalRState, isLive, isInitialLoad]);

  return (
    <form
      onSubmit={handleSubmit}
      className="relative w-full h-full flex flex-col gap-2"
    >
      <div className="w-full items-end flex gap-1">
        <div className="grow mr-1">
          <Grid gutter={"xs"}>
            <Grid.Col span={5}>
              <TextInput
                label="Search"
                value={search}
                onChange={(e) => setSearch(e.currentTarget.value)}
              ></TextInput>
            </Grid.Col>
            <Grid.Col span={4}>
              <TextInput
                label="UserId"
                value={userId}
                onChange={(e) => setUserId(e.currentTarget.value)}
              ></TextInput>
            </Grid.Col>
            <Grid.Col span={3}>
              <DatePickerInput
                type="range"
                label="Date Range"
                placeholder="Pick dates range"
                clearable
                value={dateRange}
                onChange={setDateRange}
              />
            </Grid.Col>
          </Grid>
        </div>
        <div className={isLive ? "hidden" : "block"}>
          <Tooltip label={"Go"}>
            <Button type={"submit"} variant="outline">
              <IconPlayerPlayFilled size={12} />
            </Button>
          </Tooltip>
        </div>
        <div className={isLive ? "block" : "hidden"}>
          <Tooltip label={"Stop"}>
            <Button
              variant="outline"
              color={"red"}
              onClick={(e) => {
                if (isLive) {
                  e.preventDefault();
                  setIsLive(false);
                }
              }}
            >
              <IconPlayerStopFilled size={12} />
            </Button>
          </Tooltip>
        </div>
        <Tooltip label="Live">
          <Button
            variant={isLive ? `filled` : `outline`}
            onClick={() => setIsLive(true)}
          >
            <IconActivity size={16} />
          </Button>
        </Tooltip>
      </div>
      <div className="flex gap-6">
        <Checkbox
          label="All"
          checked={logLevels.all}
          onChange={(e) => handleCheckboxChange("all", e.currentTarget.checked)}
        />
        <Checkbox
          label="Critical"
          checked={logLevels.Critical}
          onChange={(e) =>
            handleCheckboxChange("Critical", e.currentTarget.checked)
          }
        />
        <Checkbox
          label="Error"
          checked={logLevels.Error}
          onChange={(e) =>
            handleCheckboxChange("Error", e.currentTarget.checked)
          }
        />
        <Checkbox
          label="Warning"
          checked={logLevels.Warning}
          onChange={(e) =>
            handleCheckboxChange("Warning", e.currentTarget.checked)
          }
        />
        <Checkbox
          label="Information"
          checked={logLevels.Information}
          onChange={(e) =>
            handleCheckboxChange("Information", e.currentTarget.checked)
          }
        />
        <Checkbox
          label="Debug"
          checked={logLevels.Debug}
          onChange={(e) =>
            handleCheckboxChange("Debug", e.currentTarget.checked)
          }
        />
      </div>
      <Paper
        styles={{
          root: {
            flexGrow: 1,
            overflowY: "scroll",
            position: "relative",
          },
        }}
        // p="xs"
        withBorder
      >
        {isLoading && (
          <div className="absolute w-full h-full flex justify-center items-center">
            <Loader />
          </div>
        )}
        <Box p="xs">
          {logs.map((log) => (
            <LogItem key={log.LogId} log={log} />
          ))}
        </Box>
      </Paper>
    </form>
  );
};

export default LogsViewer;
