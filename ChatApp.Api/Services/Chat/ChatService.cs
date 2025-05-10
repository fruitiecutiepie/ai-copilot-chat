using ChatApp.Api.Services.Chat.Db;
using ChatApp.Api.Services.Chat.Models;
using ChatApp.Api.Services.FileStorage;

namespace ChatApp.Api.Services.Chat;

public class ChatService : IChatService
{
  readonly IChatDb _db;
  readonly IFileStorage _store;

  public ChatService(IChatDb db, IFileStorage store)
    => (_db, _store) = (db, store);

  public async Task<ChatMessage> SetChatMessageAsync(
    string convId, string userId, string content, IEnumerable<IFormFile> files)
  {
    // save attachments
    var paths = new List<string>();
    foreach (var f in files)
      paths.Add(await _store.SetChatMessageAttachment(convId, f.OpenReadStream(), f.FileName));

    // create & persist message
    var msg = new ChatMessage
    {
      Id = NanoidDotNet.Nanoid.Generate(),
      ConvId = convId,
      UserId = userId,
      Content = content,
      Timestamp = DateTime.UtcNow
    };
    await _db.SetChatMessage(msg);

    // persist attachments records
    foreach (var p in paths)
      await _db.SetChatMessageAttachment(msg.Id, p);

    return msg;
  }

  public Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsync(string convId, int limit = 100)
    => _db.GetChatMessages(convId, limit);
}
