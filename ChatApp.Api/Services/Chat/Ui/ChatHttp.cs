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

  [HttpPost]
  public async Task<IActionResult> SetChatMessageAsync(
    [FromForm] string userId,
    [FromForm] string convId,
    [FromForm] string content,
    [FromForm] List<IFormFile> attachments
  ) {
    var uploadsPath = Path.Combine(_env.ContentRootPath, "UserData/uploads", convId);
    Directory.CreateDirectory(uploadsPath);

    var messageId = NanoidDotNet.Nanoid.Generate();

    var atchs = new List<ChatMessageAttachment>();
    foreach (var atch in attachments)
    {
      if (atch.Length > 0)
      {
        var fileName = $"{NanoidDotNet.Nanoid.Generate()}-{atch.FileName}";

        var filePath = Path.Combine(uploadsPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await atch.CopyToAsync(stream);
        }

        var fileExt = Path.GetExtension(atch.FileName);
        ChatMessageAttachment.AttachmentType fileType = fileExt switch
        {
          ".txt" => ChatMessageAttachment.AttachmentType.Text,
          ".md" => ChatMessageAttachment.AttachmentType.Text,
          ".json" => ChatMessageAttachment.AttachmentType.Json,
          ".csv" => ChatMessageAttachment.AttachmentType.Csv,
          ".pdf" => ChatMessageAttachment.AttachmentType.Pdf,
          ".png" => ChatMessageAttachment.AttachmentType.Image,
          ".jpg" => ChatMessageAttachment.AttachmentType.Image,
          ".jpeg" => ChatMessageAttachment.AttachmentType.Image,
          _ => ChatMessageAttachment.AttachmentType.Other
        };

        atchs.Add(new ChatMessageAttachment
        {
          Id = NanoidDotNet.Nanoid.Generate(),
          MessageId = messageId,
          FileName = fileName,
          FileType = fileType,
          FilePath = filePath,
        });
      }
    }

    var message = new ChatMessage
    {
      Id = messageId,
      UserId = userId,
      ConvId = convId,
      Content = content,
      Timestamp = DateTime.UtcNow,
      Attachments = atchs ?? new List<ChatMessageAttachment>()
    };

    await _chatService.SetChatMessageAsync(message);

    return Ok(new
    {
      Data = message
    });
  }
}
