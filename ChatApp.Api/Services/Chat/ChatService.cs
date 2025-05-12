using ChatApp.Api.Models;
using ChatApp.Api.Ports;
// using ChatApp.Api.Services.FsService;

namespace ChatApp.Api.Services.Chat;

public class ChatService : IChatService
{
  readonly IDbService _db;
  // readonly IFsService _store;
  private readonly ILlmService _llm;

  public ChatService(
    IDbService db,
    // IFsService store,
    ILlmService llm
  ) {
    _db = db;
    // _store = store;
    _llm = llm;
  }

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

    int chunkIdxContent = 0;
    var vec = await _llm.GetEmbeddingAsync(Llm.EmbeddingInputType.Text, msg.Content);
    foreach (var embedding in vec)
    {
      await _db.SetDocChunksAsync($"{msg.Id}:{chunkIdxContent++}", msg.Content, embedding);
    }

    if (msg.Attachments.Count > 0)
    {
      foreach (var a in msg.Attachments)
      {
        int chunkIdxAtch = 0;
        var embedInputType = a.FileType == ChatMessageAttachment.AttachmentType.Image
          ? Llm.EmbeddingInputType.Image
          : Llm.EmbeddingInputType.Pdf;
        var embeddings = await _llm.GetEmbeddingAsync(embedInputType, a.FilePath);
        foreach (var embedding in embeddings)
        {
          await _db.SetDocChunksAsync($"{msg.Id}:{chunkIdxAtch++}", a.FilePath, embedding);
        }
      }
    }

    return msg;
  }
}
