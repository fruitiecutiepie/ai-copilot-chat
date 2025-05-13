using ChatApp.Api.Models;

namespace ChatApp.Api.Ports;

public interface IDbService
{
  Task SetDbChatMessages(IEnumerable<ChatMessage> messages);
  Task SetDbChatMessageAttachments(IEnumerable<ChatMessageAttachment> attachments);
  Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(string convId);
  Task<IEnumerable<string>> GetDbChatConversations(string userId);

  Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k);
  Task SetDocChunksAsync(string userId, string convId, string chunk, float[] embedding);
}
