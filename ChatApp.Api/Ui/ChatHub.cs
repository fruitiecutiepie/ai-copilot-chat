using System.Text;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Ui;

public class ChatHub : Hub
{
  private readonly IDbService _db;
  readonly ILlmService _llm;

  public ChatHub(
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
    string senderId,
    string convId,
    string content
  ) {
    var sb = new StringBuilder();
    await foreach (var chunk in _llm.GetCompletionStreamAsync(convId, senderId, content))
    {
      sb.Append(chunk);
      await Clients.Caller.SendAsync("LlmRecvMessageStream", chunk);
    }

    var msg = new ChatMessage
    {
      Id = NanoidDotNet.Nanoid.Generate(),
      ConvId = convId,
      SenderId = "assistant",
      ReceiverId = senderId,
      Content = sb.ToString(),
      Timestamp = DateTime.UtcNow,
      Attachments = new List<ChatMessageAttachment>()
    };
    await _db.SetDbChatMessagesWithAttachments(new[] { msg });
    await Clients.Caller.SendAsync("LlmRecvMessage", msg);
  }
}
