using ChatApp.Api.Models;

namespace ChatApp.Api.Ports;

public interface IChatService
{
  Task<ChatMessage> SetChatMessageAsync(
    ChatMessage message
  );
  Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsync(
    string convId
  );
}
