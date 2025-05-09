"use client";
import { useEffect, useState, useRef } from "react";
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

import ChatWindow from "@/components/ChatWindow";
import { ChatMessage, ChatMessageSchema } from "@/types";
import { res, resAsync } from "@/common/res";

export const SERVER_DOMAIN = "localhost:5000";

export const USER_ID = "A1b2C3d4E5f6G7h8I9j0K";
export const CONV_ID = "XyZ123abcDEF456ghiJKL";

export default function Home() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const connRef = useRef<HubConnection>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const [showSpacer, setShowSpacer] = useState(false);
  useEffect(() => {
    function onResize() {
      setShowSpacer(window.innerWidth >= 1024);
    }
    window.addEventListener("resize", onResize);
    onResize();              // set initial
    return () => window.removeEventListener("resize", onResize);
  }, []);

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

  const send = async () => {
    if (!inputRef.current) return;
    const text = inputRef.current.value.trim();
    if (!text && selectedFiles.length === 0) return;

    let attachmentUrls: string[] = [];
    if (selectedFiles.length > 0) {
      const form = new FormData();
      selectedFiles.forEach((f) => form.append("files", f));
      try {
        const res = await fetch(`https://${SERVER_DOMAIN}/api/uploads`, {
          method: "POST",
          body: form,
        });
        const json = await res.json();
        attachmentUrls = json.urls;
      } catch (err) {
        console.error("Upload failed", err);
      }
    }

    await connRef.current!.invoke("SendMessage",
      USER_ID,
      CONV_ID,
      text,
      attachmentUrls.join(","),
    );
    inputRef.current.value = "";
    setSelectedFiles([]);
  };

  return (
    <div
      className="flex flex-col w-full p-10 space-y-4"
    >
      <div
        className="p-4 grid grid-cols-2 lg:grid-cols-3 gap-4"
        style={{ fontSize: "0.8rem" }}
      >
        {showSpacer && <div />}
        <div
          className="flex flex-col w-full border rounded p-4 border-gray-300"
        >
          <div
            className="flex items-center justify-between pb-4 h-16"
          >
            <h1
              className="text-xl font-bold"
            >
              Chat App
            </h1>
          </div>
          <div
            className="flex flex-col h-full space-y-2"
          >
            {connRef.current ? (
              <div>
                <ChatWindow
                  messages={messages}
                />
                <div
                  className="flex flex-col space-y-2"
                >
                  <input
                    type="file"
                    multiple
                    accept=".pdf,image/*,.txt"
                    onChange={(e) =>
                      setSelectedFiles(e.target.files ? Array.from(e.target.files) : [])
                    }
                    className="border border-gray-300 rounded px-3 py-2 bg-gray-50 dark:bg-gray-900"
                    style={{ fontSize: "0.8rem" }}
                  />
                  <div
                    className="flex"
                  >
                    <input
                      ref={inputRef}
                      type="text"
                      className="flex-1 border border-gray-300 rounded-l px-3 py-2"
                      placeholder="Type a message"
                      onKeyDown={(e) => e.key === "Enter" && send()}
                    />
                    <button
                      onClick={send}
                      className="bg-blue-600 text-white px-4 rounded-r"
                    >
                      Send
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <div
                className="flex items-center justify-center h-full"
                style={{ fontSize: "0.8rem" }}
              >
                <div
                  className="text-xs text-gray-500"
                >
                  Connecting to chat...
                </div>
                <div
                  className="w-2 h-2 rounded-full bg-green-500 ml-2"
                  style={{
                    animation: "pulse 1s infinite",
                    opacity: 0.5,
                  }}
                />
              </div>
            )}
          </div>
          </div>
        )}
      </div>
    </div>
  );
}
