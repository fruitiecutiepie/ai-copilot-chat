namespace ChatApp.Api.Services.FileStorage;

public interface IFileStorage
{
  Task<string> SetChatMessageAttachment(string conversationId, Stream fileStream, string filename);
  Task<Stream> GetChatMessageAttachment(string path);
  Task DelChatMessageAttachment(string path);
}