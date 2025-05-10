using ChatApp.Api.Services.Chat.Models;

namespace ChatApp.Api.Services.Chat;

public interface IChatService
{
  Task<ChatMessage> SetChatMessageAsync(
    string conversationId,
    string senderId,
    string content,
    IEnumerable<IFormFile> attachments);
  Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsync(
    string conversationId,
    int limit = 100);
}
