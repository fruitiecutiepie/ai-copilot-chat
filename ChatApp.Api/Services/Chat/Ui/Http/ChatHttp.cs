using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat;

[ApiController]
[Route("api/chat")]
public partial class ChatController : ControllerBase
{
  private readonly IChatService _chatService;

  public ChatController(IChatService chatService)
    => _chatService = chatService;
}