using ChatApp.Api.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Ui;

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
