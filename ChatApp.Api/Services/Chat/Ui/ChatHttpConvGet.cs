using ChatApp.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat.Ui;

public partial class ChatController
{
  [HttpGet("{convId}")]
  public async Task<ActionResult<IEnumerable<ChatMessageSplit>>> GetChatMessagesAsync(
    [FromRoute(Name = "convId")] string convId
  ) {
    var messagesSplit = await _chatService.GetChatMessagesAsync(convId);
    return Ok(messagesSplit);
  }
}
