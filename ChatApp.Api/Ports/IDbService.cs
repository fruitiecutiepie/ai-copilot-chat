using ChatApp.Api.Models;

namespace ChatApp.Api.Ports;

public interface IDbService
{
  Task SetDbChatMessageAsync(ChatMessage msg);
  Task SetDbChatMessageAttachmentAsync(string messageId, string filePath);
  Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(string conversationId, int limit);
  Task<IEnumerable<string>> GetDbChatConversationsAsync(string userId);

  Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k);
  Task SetDocChunksAsync(string id, string content, float[] embedding);
}
