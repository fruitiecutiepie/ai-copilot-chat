import { z } from "zod";

export const AttachmentFileTypeSchema = z.enum([
  "Text",
  "Json",
  "Csv",
  "Pdf",
  "Image",
  "Other",
]);

export const ChatMessageAttachmentSchema = z.object({
  id: z.string(),
  messageId: z.string(),
  fileName: z.string(),
  fileType: AttachmentFileTypeSchema,
  filePath: z.string(),
});
export type ChatMessageAttachment = z.infer<typeof ChatMessageAttachmentSchema>;

export const ChatMessageSchema = z.object({
  id: z.string(),
  userId: z.string(),

  convId: z.string(),

  content: z.string(),
  timestamp: z.string(),

  attachments: z.array(ChatMessageAttachmentSchema)
});
export type ChatMessage = z.infer<typeof ChatMessageSchema>;
