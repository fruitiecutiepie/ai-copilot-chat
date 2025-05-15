using ChatApp.Api.Models;
using ChatApp.Api.Ports;

namespace ChatApp.Api.Services.Chat;

public class ChatMessageSplit
{
  public List<ChatMessage> ChatMessageHumanHuman { get; set; } = new();
  public List<ChatMessage> ChatMessageHumanAi { get; set; } = new();
}

public class ChatService : IChatService
{
  // In real-world, auth requests with JWT tokens and get userId from token
  private const string SENDER_ID = "A1b2C3d4E5f6G7h8I9j0K";

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

  public async Task<ChatMessageSplit> GetChatMessagesAsync(
    string convId
  ) {
    var all = await _db.GetDbChatMessagesAsync(convId);
    var humanHumanMessages = all.Where(m =>
      (m.SenderId == SENDER_ID && m.ReceiverId != "assistant")
      || (m.SenderId != "assistant" && m.ReceiverId == SENDER_ID)
    );
    var humanAiMessages = all.Where(m =>
      (m.SenderId == SENDER_ID && m.ReceiverId == "assistant")
      || (m.SenderId == "assistant" && m.ReceiverId == SENDER_ID)
    );

    return new ChatMessageSplit
    {
      ChatMessageHumanHuman = humanHumanMessages.ToList(),
      ChatMessageHumanAi = humanAiMessages.ToList()
    };
  }

  public async Task<ChatMessage> SetChatMessageAsync(
    ChatMessage msg
  ) {
    await _db.SetDbChatMessagesWithAttachments(new[] { msg });
    if (msg.ReceiverId == "assistant" || msg.SenderId == "assistant")
    {
      return msg;
    }

    var textEmbeddings = await _llm.GetEmbeddingAsync(
      Llm.EmbeddingInputType.Text,
      msg.Content
    );
    foreach (var r in textEmbeddings)
      await _db.SetDocChunksAsync(
        msg.SenderId,
        msg.ConvId,
        r.Chunk,
        r.Embedding
      );

    // embed & store chunks for each attachment
    foreach (var a in msg.Attachments)
    {
      var type = a.FileType switch
      {
        ChatMessageAttachment.AttachmentType.Pdf => Llm.EmbeddingInputType.Pdf,
        ChatMessageAttachment.AttachmentType.Image => Llm.EmbeddingInputType.Image,
        _ => Llm.EmbeddingInputType.Text
      };
      var path = a.FileType == ChatMessageAttachment.AttachmentType.Text
        ? a.FilePath
        : Path.Combine("UserData/uploads", msg.ConvId, a.FilePath).Replace("\\", "/");

      var attEmbeddings = await _llm.GetEmbeddingAsync(type, path);
      foreach (var r in attEmbeddings)
        await _db.SetDocChunksAsync(
          msg.SenderId,
          msg.ConvId,
          r.Chunk,
          r.Embedding
        );
    }
    // await _hub.Clients.Group(msg.ConvId).SendAsync("ChatRecvMessage", msg);

    return msg;
  }
}
