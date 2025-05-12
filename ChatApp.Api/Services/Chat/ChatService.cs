using ChatApp.Api.Models;
using ChatApp.Api.Ports;
// using ChatApp.Api.Services.FsService;

namespace ChatApp.Api.Services.Chat;

public class ChatService : IChatService
{
  readonly IChatDb _db;
  // readonly IFsService _store;

  public Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsyncAsync(string convId, int limit = 100)
    => _db.GetDbChatMessagesAsync(convId, limit);

  public async Task<ChatMessage> SetChatMessageAsync(
    string convId, string userId, string content, string[] filePaths
  )
  {
    // var paths = new List<string>();
    // foreach (var f in files)
    //   paths.Add(await _store.SetChatMessageAttachment(convId, f.OpenReadStream(), f.FileName));

    var msg = new ChatMessage
    {
      Id = NanoidDotNet.Nanoid.Generate(),
      ConvId = convId,
      UserId = userId,
      Content = content,
      Timestamp = DateTime.UtcNow
    };
    await _db.SetDbChatMessageAsync(msg);

    foreach (var path in filePaths)
      await _db.SetDbChatMessageAttachmentAsync(msg.Id, path);

    return msg;
  }
}
