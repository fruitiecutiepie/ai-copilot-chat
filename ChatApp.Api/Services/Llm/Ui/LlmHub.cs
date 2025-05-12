using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Services.Llm.Ui;

public class LlmHub(ILlmService llm) : Hub
{
  readonly ILlmService _llm = llm;

  public override async Task OnConnectedAsync()
  {
    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    await base.OnDisconnectedAsync(exception);
  }

  public async Task LlmSendMessage(
    string convId,
    string userId,
    string content
  ) {
    await foreach (var chunk in _llm.StreamCompletionAsync(convId, userId, content))
      await Clients.Caller.SendAsync("LlmRecvMessage", chunk);
  }
}
