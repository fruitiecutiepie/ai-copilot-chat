using ChatApp.Api.Services.Chat.Models;

namespace ChatApp.Api.Services.Chat.Db;

public interface IChatDb
{
  Task SetChatMessage(ChatMessage msg);
  Task SetChatMessageAttachment(string messageId, string filePath);
  Task<IReadOnlyList<ChatMessage>> GetChatMessages(string conversationId, int limit);
  Task<IEnumerable<string>> GetChatConversations(string userId);
}