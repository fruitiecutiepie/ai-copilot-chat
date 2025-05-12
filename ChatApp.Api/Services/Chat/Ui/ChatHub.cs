using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Services.Chat.Ui;

public class ChatHub : Hub
{
  readonly IChatService _chat;

  public ChatHub(IChatService chat) => _chat = chat;

  public override async Task OnConnectedAsync()
  {
    var http = Context.GetHttpContext()!;
    var convId = http.Request.Query["convId"];
    if (string.IsNullOrEmpty(convId))
    {
      throw new ArgumentException(
        "Missing conversation ID in query string");
    }
    // SignalR automatically removes connection from groups
    await Groups.AddToGroupAsync(Context.ConnectionId, convId!);
    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    await base.OnDisconnectedAsync(exception);
  }

  public async Task ChatSendMessage(
    string userId,
    string convId,
    string content,
    string[] attachmentUrls
  )
  {
    var msg = await _chat.SetChatMessageAsync(convId, userId, content, attachmentUrls);
    await Clients.Group(convId).SendAsync("ChatRecvMessage", msg);
  }
}
