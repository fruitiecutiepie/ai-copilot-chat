import { CONV_ID, SERVER_DOMAIN, USER_ID } from "@/app/page";
import { ChatMessage } from "@/types";
import { useCallback } from "react";
import Image from "next/image";

type ChatWindowProps = {
  messages: ChatMessage[];
};

export default function ChatWindow(
  { messages }: ChatWindowProps
) {
  // helper to fetch a blob and either open or download it
  const handleDownload = useCallback(async (fileName: string, viewInline = false) => {
    try {
      const url = `http://${SERVER_DOMAIN}/uploads/${CONV_ID}/${fileName}`;
      const res = await fetch(url);
      const blob = await res.blob();
      const blobUrl = URL.createObjectURL(blob);
      if (viewInline) {
        window.open(blobUrl, "_blank");
      } else {
        const a = document.createElement("a");
        a.href = blobUrl;
        a.download = url.split("/").pop() || "file";
        document.body.appendChild(a);
        a.click();
        a.remove();
      }
      URL.revokeObjectURL(blobUrl);
    } catch (err) {
      console.error("File download error:", err);
    }
  }, []);

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

              {/* attachments */}
              {msg.attachments?.map((atch, i) => {
                const url = atch.filePath;
                if (!url) return null;
                if (url.endsWith(".pdf")) {
                  return (
                    <button
                      key={i}
                      onClick={() => handleDownload(url, true)}
                      className="block text-blue-600 underline mt-1"
                    >
                      View PDF
                    </button>
                  );
                }
                if (/\.(jpe?g|png|gif)$/i.test(url)) {
                  return (
                    <Image
                      key={i}
                      src={`http://${SERVER_DOMAIN}/uploads/${CONV_ID}/${url}`}
                      alt={`attach-${i}`}
                      className="mt-2 max-w-full h-auto rounded"
                      style={{ maxWidth: "100%", height: "auto" }}
                      width={200}
                      height={200}
                    />
                  );
                }
                if (url.endsWith(".txt")) {
                  return (
                    <button
                      key={i}
                      onClick={() => handleDownload(url)}
                      className="block text-blue-600 underline mt-1"
                    >
                      Download text file
                    </button>
                  );
                }
                // fallback
                return (
                  <button
                    key={i}
                    onClick={() => handleDownload(url)}
                    className="block text-blue-600 underline mt-1"
                  >
                    Download file
                  </button>
                );
              })}

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
