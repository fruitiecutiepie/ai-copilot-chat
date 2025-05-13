"use client";
import { useEffect, useState, useRef } from "react";
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

import ChatWindow from "@/components/ChatWindow";
import { ChatMessage, ChatMessageSchema } from "@/types";
import { res, resAsync } from "@/common/res";
import { nanoid } from "nanoid";

export const SERVER_DOMAIN = "localhost:5000";

export const USER_ID = "A1b2C3d4E5f6G7h8I9j0K";
export const AI_USER_ID = "AI_ASSISTANT";
export const CONV_ID = "XyZ123abcDEF456ghiJKL";

export default function Home() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [messagesLlm, setMessagesLlm] = useState<ChatMessage[]>([]);

  const [chatLlmOpen, setChatLlmOpen] = useState(false);

  const [selectedFilesChat, setSelectedFilesChat] = useState<File[]>([]);

  const inputRefChat = useRef<HTMLInputElement>(null);
  const inputRefLlm = useRef<HTMLInputElement>(null);

  const connRefChat = useRef<HubConnection>(null);
  const connRefLlm = useRef<HubConnection>(null);

  const [showSpacer, setShowSpacer] = useState(false);
  useEffect(() => {
    function onResize() {
      setShowSpacer(window.innerWidth >= 1024);
    }
    window.addEventListener("resize", onResize);
    onResize(); // set initial
    return () => window.removeEventListener("resize", onResize);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch(`http://${SERVER_DOMAIN}/api/chat/${CONV_ID}`);
        if (!res.ok) throw new Error(res.statusText);
        const json = await res.json();
        const parsed = ChatMessageSchema.array().safeParse(json);
        if (!parsed.success) throw parsed.error;
        if (cancelled) return;

        const all = parsed.data;
        console.log("ChatMessageSchema", JSON.stringify(all, null, 2));
        setMessages(all.filter(m => m.userId !== AI_USER_ID));
        setMessagesLlm(all.filter(m => m.userId === AI_USER_ID));
      } catch (err) {
        console.error("History fetch error:", err);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    const connRes = res(() => new HubConnectionBuilder()
      .withUrl(`http://${SERVER_DOMAIN}/hubs/chat?convId=${CONV_ID}`, {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()
    );
    if (!connRes.is_ok) {
      console.error("connRes error: ", connRes.err);
      return;
    }
    const conn = connRes.ok;

    let mounted = true;
    (async () => {
      const connRes = await resAsync(() => conn.start());
      if (!connRes.is_ok) {
        console.error("Connection error: ", connRes.err);
        return;
      }
      if (!mounted) return;

      connRefChat.current = conn;
      console.log("Connected to SignalR conn Chat");

      conn.on("ChatRecvMessage", (msg: ChatMessage) => {
        console.log("ChatRecvMessage", JSON.stringify(msg, null, 2));
        if (!mounted) return;
        setMessages((prev) => [...prev, msg]);
      });
    })();

    return (() => {
      mounted = false;
      connRefChat.current = null;
      // remove handlers and stop the connection
      conn.off("ChatRecvMessage");
      conn.stop().catch((err) => {
        console.error("Error stopping connection: ", err);
      });
    });
  }, []);

  const sendChat = async () => {
    if (!inputRefChat.current) return;
    const text = inputRefChat.current.value.trim();
    if (!text && selectedFilesChat.length === 0) return;

    const form = new FormData();
    form.append("convId", CONV_ID);
    form.append("userId", USER_ID);
    form.append("content", text || "");
    if (selectedFilesChat.length > 0) {
      selectedFilesChat.forEach((f) => form.append("attachments", f));
    }

    const res = await fetch(
      `http://${SERVER_DOMAIN}/api/chat`,
      { method: "POST", body: form }
    );
    if (!res.ok) throw new Error(await res.text());

    try {
      const res = await fetch(`http://${SERVER_DOMAIN}/api/chat`, {
        method: "POST",
        body: form,
      });
      if (!res.ok) throw new Error(res.statusText);

      const resJson = await res.json();

      const resParsed = ChatMessageSchema.safeParse(resJson.data);
      if (!resParsed.success) {
        console.error("ChatMessageSchema error: ", resParsed.error);
        throw new Error("Invalid response");
      }

      setMessages((prev) => [...prev, resParsed.data]);
    } catch (err) {
      console.error("Upload failed", err);
    }

    inputRefChat.current.value = "";
    setSelectedFilesChat([]);
  };

  useEffect(() => {
    const connRes = res(() => new HubConnectionBuilder()
      .withUrl(`http://${SERVER_DOMAIN}/hubs/llm`, {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()
    );
    if (!connRes.is_ok) {
      console.error("connRes error: ", connRes.err);
      return;
    }
    const conn = connRes.ok;

    let mounted = false;

    const init = async () => {
      const connRes = await resAsync(() => conn.start());
      if (!connRes.is_ok) {
        console.error("Connection error: ", connRes.err);
        return;
      }
      if (mounted) return;

      connRefLlm.current = conn;
      console.log("Connected to SignalR conn LLM");

      conn.on("LlmRecvMessage", (chunk: string) =>
        setMessagesLlm(prev => {
          // first chunk or starting a new response?
          const last = prev[prev.length - 1];
          if (!last || last.userId !== AI_USER_ID) {
            // create a brand-new ChatMessage
            const newMsg: ChatMessage = {
              id: nanoid(),
              userId: AI_USER_ID,
              convId: CONV_ID,
              content: chunk,
              timestamp: new Date().toISOString(),
              attachments: []
            };
            return [...prev, newMsg];
          } else {
            // append chunk to last message
            const updated = [...prev];
            updated[updated.length - 1] = {
              ...last,
              content: last.content + chunk
            };
            return updated;
          }
        })
      );
    }
    init();

    return (() => {
      mounted = true;
      connRefLlm.current = null;
      // remove handlers and stop the connection
      conn.off("LlmRecvMessage");
      conn.stop().catch((err) => {
        console.error("Error stopping connection: ", err);
      });
    });
  }, []);

  const sendChatLlm = async () => {
    if (!inputRefLlm.current) return;
    const text = inputRefLlm.current.value.trim();
    if (!text) return;

    setMessagesLlm((prev) => [
      ...prev,
      {
        id: nanoid(),
        userId: USER_ID,
        convId: CONV_ID,
        content: text,
        timestamp: new Date().toISOString(),
        attachments: []
      }
    ]);

    await connRefLlm.current!.invoke("LlmSendMessage",
      USER_ID,
      CONV_ID,
      text
    );
    inputRefLlm.current.value = "";
    setSelectedFilesChat([]);
  };

  return (
    <div
      className="flex flex-col w-full p-10 space-y-4"
    >
      {/* <div
        className="flex items-center justify-center mb-4"
        style={{ fontSize: "0.8rem" }}
      >
        <div
          className="text-xs text-gray-500"
        >
          {connRefChat.current ? "Connected" : "Connecting..."}
        </div>
        <div
          className="w-2 h-2 rounded-full bg-green-500 ml-2"
          style={{
            animation: connRefChat.current ? "none" : "pulse 1s infinite",
            opacity: connRefChat.current ? 1 : 0.5,
          }}
        />
      </div> */}
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
            {connRefChat.current && (
              <button
                className="bg-blue-600 text-white px-4 py-2 rounded"
                onClick={() => {
                  setChatLlmOpen(!chatLlmOpen);
                  if (inputRefChat.current) {
                    inputRefChat.current.value = "";
                  }
                }}
                title={chatLlmOpen ? "Close Copilot" : "Open Copilot"}
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  strokeWidth="1.5"
                  stroke="currentColor"
                  className="w-5 h-5"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M4.26 10.147a60.436 60.436 0 00-.491 6.347A48.627 48.627 0 0112 20.904a48.627 48.627 0 018.232-4.41 60.46 60.46 0 00-.491-6.347m-15.482 0a50.57 50.57 0 00-2.658-.813A59.905 59.905 0 0112 3.493a59.902 59.902 0 0110.399 5.84c-.896.248-1.783.52-2.658.814m-15.482 0A50.697 50.697 0 0112 13.489a50.702 50.702 0 017.74-3.342M6.75 15a.75.75 0 100-1.5.75.75 0 000 1.5zm0 0v-3.675A55.378 55.378 0 0112 8.443m-7.007 11.55A5.981 5.981 0 006.75 15.75v-1.5"
                  />
                </svg>
              </button>
            )}
          </div>
          <div
            className="flex flex-col h-full space-y-2"
          >
            {connRefChat.current ? (
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
                      setSelectedFilesChat(e.target.files ? Array.from(e.target.files) : [])
                    }
                    className="border border-gray-300 rounded px-3 py-2 bg-gray-50 dark:bg-gray-900"
                    style={{ fontSize: "0.8rem" }}
                  />
                  <div
                    className="flex"
                  >
                    <input
                      ref={inputRefChat}
                      type="text"
                      className="flex-1 border border-gray-300 rounded-l px-3 py-2"
                      placeholder="Type a message"
                      onKeyDown={(e) => e.key === "Enter" && sendChat()}
                    />
                    <button
                      onClick={sendChat}
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
        {chatLlmOpen && (
          <div
            className="flex flex-col w-full border rounded p-4 border-gray-300"
          >
            <div
              className="flex items-center justify-between pb-4 h-16"
            >
              <h1
                className="text-xl font-bold"
              >
                AI Copilot
              </h1>
            </div>
            <div
              className="flex flex-col h-full space-y-2"
            >
              <ChatWindow
                messages={messagesLlm}
              />
            </div>
            {connRefChat.current && (
              <div
                className="flex flex-col space-y-2"
              >
                <div
                  className="flex"
                >
                  <input
                    ref={inputRefLlm}
                    type="text"
                    className="flex-1 border border-gray-300 rounded-l px-3 py-2"
                    placeholder="Type a message"
                    onKeyDown={(e) => e.key === "Enter" && sendChatLlm()}
                  />
                  <button
                    onClick={sendChatLlm}
                    className="bg-blue-600 text-white px-4 rounded-r"
                  >
                    Send
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
