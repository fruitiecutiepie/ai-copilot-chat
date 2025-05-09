"use client";
import { useEffect, useState, useRef } from "react";
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

export const SERVER_DOMAIN = "localhost:5000";

export const USER_ID = "A1b2C3d4E5f6G7h8I9j0K";
export const CONV_ID = "XyZ123abcDEF456ghiJKL";

export default function Home() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const connRef = useRef<HubConnection>(null);
  useEffect(() => {
    const connRes = res(() => new HubConnectionBuilder()
      .withUrl(`http://${SERVER_DOMAIN}/hubs`, {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true
      })
      .configureLogging(LogLevel.Information)
      .build()
    );
    if (!connRes.is_ok) {
      console.error("connRes error: ", connRes.err);
      return;
    }
    const conn = connRes.ok;

    let didCancel = false;

    const init = async () => {
      const connRes = await resAsync(() => conn.start());
      if (!connRes.is_ok) {
        console.error("Connection error: ", connRes.err);
        return;
      }
      if (didCancel) return;

      connRef.current = conn;
      console.log("Connected to SignalR conn");

      const joinRes = await resAsync(() =>
        conn.invoke("JoinConversation", CONV_ID)
      );
      if (!joinRes.is_ok) {
        console.error("Join conversation error: ", joinRes.err);
        return;
      }
      console.log("Joined conversation: ", CONV_ID);
      const historyRes = await resAsync(() =>
        conn.invoke("GetHistory", CONV_ID, 1, 50)
      );
      if (!historyRes.is_ok) {
        console.error("Get history error: ", historyRes.err);
        return;
      }
      const historyParseRes = ChatMessageSchema.array().safeParse(historyRes.ok);
      if (!historyParseRes.success) {
        console.error("Invalid history format: ", historyParseRes.error);
        return;
      }
      const history = historyParseRes.data;
      setMessages(history);
      console.log("History: ", JSON.stringify(history, null, 2));

      conn.on("ReceiveMessage", (msg: ChatMessage) =>
        setMessages((prev) => [...prev, msg])
      );
    }
    init();

    return (() => {
      didCancel = true;
      connRef.current = null;
      // remove handlers and stop the connection
      conn.off("ReceiveMessage");
      conn.stop().catch((err) => {
        console.error("Error stopping connection: ", err);
      });
    });
  }, []);

  return (
    <div
      className=""
    >
      Hello world!
    </div>
  );
}
