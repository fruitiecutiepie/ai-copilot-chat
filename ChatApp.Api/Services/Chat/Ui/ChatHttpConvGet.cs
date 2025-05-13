using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat.Ui;

public class ChatMessageDto
{
  public string Id { get; set; } = default!;
  public string UserId { get; set; } = default!;
  public string ConvId { get; set; } = default!;
  public string Content { get; set; } = default!;
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public IEnumerable<AttachmentDto> Attachments { get; set; } = Enumerable.Empty<AttachmentDto>();
}

public class AttachmentDto
{
  public string Id { get; set; } = default!;
  public string MessageId { get; set; } = default!;
  public string FileName { get; set; } = default!;
  public string FileType { get; set; } = default!;
  public string FilePath { get; set; } = default!;
}

public partial class ChatController
{
  [HttpGet("{convId}")]
  public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetChatMessagesAsync(
    [FromRoute(Name = "convId")] string convId
  ) {
    var messages = await _chatService.GetChatMessagesAsync(convId);
    var result = messages.Select(m => new ChatMessageDto
    {
      Id = m.Id,
      UserId = m.UserId,
      ConvId = m.ConvId,
      Content = m.Content,
      Timestamp = m.Timestamp,
      Attachments = m.Attachments.Select(a => new AttachmentDto {
        Id = a.Id,
        MessageId = a.MessageId,
        FileName = a.FileName,
        FileType = a.FileType.ToString(),
        // FilePath  = Fs.FsService.FileNameToPublicUrl(Request, a.FileName)
        FilePath = a.FileName
      })
      .ToList()
    });
    return Ok(result);
  }
}
