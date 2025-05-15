"use client";
import { useEffect, useRef } from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel
} from "@microsoft/signalr";

export function useSignalRConnection(
  url: string
): HubConnection | null {
  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(url, {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    let mounted = true;
    connection
      .start()
      .then(() => {
        if (mounted) connRef.current = connection;
      })
      .catch(console.error);

    return () => {
      mounted = false;
      connection.stop().catch(console.error);
      connRef.current = null;
    };
  }, [url]);

  return connRef.current;
}
