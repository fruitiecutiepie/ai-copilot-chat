using ChatApp.Api.Models;

namespace ChatApp.Api.Services.Chat;

public interface IChatService
{
  Task<ChatMessage> SetChatMessageAsync(
    string convId,
    string userId,
    string content,
    string[] attachmentsUrls
  );
  Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsyncAsync(
    string convId,
    int limit = 100
  );
}
