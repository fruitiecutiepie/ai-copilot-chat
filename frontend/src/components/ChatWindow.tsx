import { USER_ID } from "@/app/page";
import { ChatMessage } from "@/types";

type ChatWindowProps = {
  messages: ChatMessage[];
};

export default function ChatWindow(
  { messages }: ChatWindowProps
) {
  return (
    <div className="h-[35rem] overflow-y-auto border border-gray-300 p-4">
      {messages.map((msg, idx) => (
        <div
          key={idx}
          className={`mb-2 flex flex-row ${msg.userId === USER_ID ? "justify-end" : ""
            }`}
        >
          <div
            className={`flex flex-col py-1 ${msg.userId === USER_ID ? "items-end" : "items-start"
              }`}
          >
            {msg.userId !== USER_ID && (
              <div className="flex items-center mb-1" style={{ fontSize: "0.8rem" }}>
                <div className="text-xs text-gray-500">{msg.userId}</div>
              </div>
            )}
            <div className="rounded px-3 py-1 max-w-xs break-words bg-blue-100 dark:bg-gray-500">
              {msg.content}
              <div className="text-xs text-right">
                {new Date(msg.timestamp).toLocaleTimeString()}
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
