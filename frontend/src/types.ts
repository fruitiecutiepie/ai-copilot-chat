import { z } from "zod";

export const ChatMessageSchema = z.object({
  id: z.string(),
  userId: z.string(),

  convId: z.string(),

  content: z.string(),
  timestamp: z.string(),

  attachmentUrls: z.array(z.string()),
});
export type ChatMessage = z.infer<typeof ChatMessageSchema>;
