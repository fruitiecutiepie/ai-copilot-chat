using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat.Ui;

[ApiController]
[Route("api/chat")]
public partial class ChatController : ControllerBase
{
  private readonly IChatService _chatService;
  private readonly IWebHostEnvironment _env;

  public ChatController(
    IChatService chatService,
    IWebHostEnvironment env
  )
  {
    _chatService = chatService;
    _env = env;
  }
}
