namespace ChatApp.Api.Ports;

public interface IFsService
{
  string FileNameToPublicUrl(string convId, string fileName);
  Task<string> SetChatMessageAttachmentAsync(
    string convId,
    Stream fileStream,
    string fileName
  );
  Task<Stream> GetChatMessageAttachment(string relativePath);
  Task DelChatMessageAttachment(string relativePath);
}
