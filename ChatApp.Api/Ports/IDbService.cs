using ChatApp.Api.Models;

namespace ChatApp.Api.Ports;

public interface IDbService
{
  Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(string convId);
  Task SetDbChatMessagesWithAttachments(IEnumerable<ChatMessage> messages);

  // Vector DB Operations
  Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k);
  Task SetDocChunksAsync(string senderId, string convId, string chunk, float[] embedding);
}
