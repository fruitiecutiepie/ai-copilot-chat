namespace ChatApp.Api.Services.FsService;

public interface IFsService
{
  Task<string> SetChatMessageAttachmentAsync(
    string convId,
    Stream fileStream,
    string fileName
  );
  Task<Stream> GetChatMessageAttachment(string path);
  Task DelChatMessageAttachment(string path);
}
