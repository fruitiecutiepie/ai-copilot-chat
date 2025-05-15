using ChatApp.Api.Models;
using ChatApp.Api.Services.Chat;

namespace ChatApp.Api.Ports;

public interface IChatService
{
  Task<ChatMessageSplit> GetChatMessagesAsync(
    string convId
  );
  Task<ChatMessage> SetChatMessageAsync(
    ChatMessage message
  );
}
