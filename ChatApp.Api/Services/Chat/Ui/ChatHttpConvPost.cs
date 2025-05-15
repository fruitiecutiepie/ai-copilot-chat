using ChatApp.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat.Ui;

public partial class ChatController
{
  [HttpPost("{convId}")]
  public async Task<IActionResult> SetChatMessageAsync(
    [FromRoute(Name = "convId")] string convId,
    [FromForm] string senderId,
    [FromForm] string receiverId,
    [FromForm] string? content,
    [FromForm] List<IFormFile> attachments
  ) {
    var uploadsPath = Path.Combine(_env.ContentRootPath, "UserData/uploads", convId);
    Directory.CreateDirectory(uploadsPath);

    var messageId = NanoidDotNet.Nanoid.Generate();

    var atchs = new List<ChatMessageAttachment>();
    foreach (var atch in attachments)
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

    var message = new ChatMessage
    {
      Id = messageId,
      ConvId = convId,
      SenderId = senderId,
      ReceiverId = receiverId,
      Content = content ?? string.Empty,
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
