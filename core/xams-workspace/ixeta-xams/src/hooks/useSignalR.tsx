// hooks/useSignalR.ts
import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useAuthContext } from "../contexts/AuthContext";
import { HubConnection } from "@microsoft/signalr";

export type useSignalRResponse = {
  send: (hubName: string, message: string) => void;
  invoke: <T = any>(hubName: string, message: string) => Promise<T>;
  on: (methodName: string, newMethod: (...args: any[]) => any) => void;
  off: (methodName: string) => void;
  hubConnection: HubConnection | null;
};

export const useSignalR = (hubUrl: string) => {
  const authContext = useAuthContext();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connectionState, setConnectionState] =
    useState<signalR.HubConnectionState>(
      signalR.HubConnectionState.Disconnected
    );
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [hubUrl]);

  const getResponse = () => {
    return {
      send: (hubName: string, message: string) => {
        connectionRef.current?.send("OnReceive", hubName, message);
      },
      invoke: (hubName: string, message: string) => {
        return connectionRef.current?.invoke("OnReceive", hubName, message);
      },
      on: (methodName: string, newMethod: (...args: any[]) => {}) => {
        connectionRef.current?.on(methodName, newMethod);
      },
      off: (methodName: string) => {
        connectionRef.current?.off(methodName);
      },
      hubConnection: connectionRef.current,
    } as useSignalRResponse;
  };

  const getConnection = async () => {
    // If we have a connection that's still good, return it
    if (
      connectionRef.current &&
      connectionRef.current.state === signalR.HubConnectionState.Connected
    ) {
      return getResponse();
    }

    // Clean up any existing connection
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop();
      } catch (error) {
        console.log("Error stopping existing connection:", error);
      }
      connectionRef.current = null;
    }

    try {
      const userId = authContext.headers?.UserId;
      const accessToken = await authContext.getAccessToken?.();
      if (userId == null && accessToken == null) {
        throw new Error(
          `SignalR connection requires either User ID or Access Token. Ensure Auth is Ready.`
        );
      }

      const url = userId ? `${hubUrl}?userid=${userId}` : hubUrl;
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(url, {
          ...(authContext.getAccessToken && {
            accessTokenFactory: async () => {
              if (authContext.getAccessToken) {
                const accessToken = await authContext.getAccessToken();
                return accessToken ?? "";
              }
              return "";
            },
          }),
          headers: { ...authContext.headers },
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // Custom retry intervals
        .build();

      // Set up connection event handlers
      newConnection.onclose(() => {
        console.log("SignalR connection closed");
        setConnectionState(signalR.HubConnectionState.Disconnected);
        connectionRef.current = null;
      });

      newConnection.onreconnecting(() => {
        console.log("SignalR reconnecting...");
        setConnectionState(signalR.HubConnectionState.Reconnecting);
      });

      newConnection.onreconnected(() => {
        console.log("SignalR reconnected successfully");
        setConnectionState(signalR.HubConnectionState.Connected);
      });

      connectionRef.current = newConnection;
      setConnectionState(signalR.HubConnectionState.Connecting);

      await connectionRef.current.start();
      setConnectionState(signalR.HubConnectionState.Connected);
      console.log("Connected to SignalR hub");
    } catch (error) {
      console.error("Error connecting to SignalR hub:", error);
      setConnectionState(signalR.HubConnectionState.Disconnected);
      connectionRef.current = null;
    }
    return getResponse();
  };

  // Auto-reconnect, if Permissions are changed such as added or removed access to new hubs
  // the connection is forcibly reset
  useEffect(() => {
    const attemptReconnection = async () => {
      if (connectionState === signalR.HubConnectionState.Disconnected) {
        console.log("Attempting automatic reconnection...");
        try {
          await getConnection();
        } catch (error) {
          console.error("Auto-reconnection failed:", error);
          // Schedule next retry
          reconnectTimeoutRef.current = setTimeout(attemptReconnection, 5000);
        }
      }
    };

    if (connectionState === signalR.HubConnectionState.Disconnected) {
      // Clear any existing timeout
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      // Schedule reconnection attempt
      reconnectTimeoutRef.current = setTimeout(attemptReconnection, 1000);
    }

    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
    };
  }, [connectionState, authContext.headers?.UserId]);

  return {
    getConnection,
    connectionState,
  };
};
