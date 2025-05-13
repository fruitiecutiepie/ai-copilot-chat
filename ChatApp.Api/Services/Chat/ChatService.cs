using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using ChatApp.Api.Services.Chat.Ui;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Services.Chat;

public class ChatService : IChatService
{
  // private readonly IHubContext<ChatHub> _hub;
  private readonly IDbService _db;
  private readonly ILlmService _llm;

  public ChatService(
    // IHubContext<ChatHub> hub,
    IDbService db,
    ILlmService llm
  ) {
    // _hub = hub;
    _db = db;
    _llm = llm;
  }

  public Task<IReadOnlyList<ChatMessage>> GetChatMessagesAsync(
    string convId
  ) => _db.GetDbChatMessagesAsync(convId);

  public async Task<ChatMessage> SetChatMessageAsync(
    ChatMessage msg
  ) {
    await _db.SetDbChatMessagesAsync(new[] { msg });
    // await _hub.Clients.Group(msg.ConvId).SendAsync("ChatRecvMessage", msg);

    var textEmbeddings = await _llm.GetEmbeddingAsync(
      Llm.EmbeddingInputType.Text,
      msg.Content
    );
    foreach (var r in textEmbeddings)
      await _db.SetDocChunksAsync(
        msg.UserId,
        msg.ConvId,
        r.Chunk,
        r.Embedding
      );

    // embed & store chunks for each attachment
    foreach (var a in msg.Attachments) {
      var type = a.FileType switch {
        ChatMessageAttachment.AttachmentType.Pdf   => Llm.EmbeddingInputType.Pdf,
        ChatMessageAttachment.AttachmentType.Image => Llm.EmbeddingInputType.Image,
        _ => Llm.EmbeddingInputType.Text
      };
      var path = a.FileType == ChatMessageAttachment.AttachmentType.Text
        ? a.FilePath
        : Fs.FsService.FileNameToLocalPath(msg.ConvId, a.FilePath);

      var attEmbeddings = await _llm.GetEmbeddingAsync(type, path);
      foreach (var r in attEmbeddings)
        await _db.SetDocChunksAsync(
          msg.UserId,
          msg.ConvId,
          r.Chunk,
          r.Embedding
        );
    }

    return msg;
  }
}
