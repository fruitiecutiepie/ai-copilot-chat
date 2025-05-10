using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Services.Chat.Ui.Ws;

public class ChatWs : Hub
{
  readonly IChatService _chat;

  public ChatWs(IChatService chat) => _chat = chat;

  public override async Task OnConnectedAsync()
  {
    var http = Context.GetHttpContext()!;
    var convId = http.Request.Query["convId"];
    if (!string.IsNullOrEmpty(convId))
    {
      throw new ArgumentException(
        "Missing conversation ID in query string");
    }
    // SignalR automatically removes connection from groups
    await Groups.AddToGroupAsync(Context.ConnectionId, convId);
    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    await base.OnDisconnectedAsync(exception);
  }

  public async Task SendMessage(
    string convId,
    string userId,
    string content,
    IEnumerable<IFormFile> attachments
  )
  {
    var msg = await _chat.SetChatMessageAsync(convId, userId, content, attachments);
    await Clients.Group(convId).SendAsync("RecvMessage", msg);
  }
}
