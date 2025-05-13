using System.Text;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Services.Llm.Ui;

public class LlmHub : Hub
{
  private readonly IDbService _db;
  readonly ILlmService _llm;

  public LlmHub(
    IDbService db,
    ILlmService llm
  ) {
    _db = db;
    _llm = llm;
  }

  public override async Task OnConnectedAsync()
  {
    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    await base.OnDisconnectedAsync(exception);
  }

  public async Task LlmSendMessage(
    string userId,
    string convId,
    string content
  ) {
    var sb = new StringBuilder();
    await foreach (var chunk in _llm.StreamCompletionAsync(convId, userId, content))
    {
      sb.Append(chunk);
      await Clients.Caller.SendAsync("LlmRecvMessage", chunk);
    }

    var msg = new ChatMessage
    {
      Id          = NanoidDotNet.Nanoid.Generate(),
      UserId      = "AI_ASSISTANT",
      ConvId      = convId,
      Content     = sb.ToString(),
      Timestamp   = DateTime.UtcNow,
      Attachments = new List<ChatMessageAttachment>()
    };
    await _db.SetDbChatMessagesAsync(new[] { msg });
  }
}
