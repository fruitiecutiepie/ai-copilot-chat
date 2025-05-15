namespace ChatApp.Api.Models;

public class ChatMessage
{
  public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();
  public string ConvId { get; set; } = NanoidDotNet.Nanoid.Generate();
  public string SenderId { get; set; } = default!;
  public string ReceiverId { get; set; } = default!;
  public string Content { get; set; } = default!;

  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public List<ChatMessageAttachment> Attachments { get; set; } = new List<ChatMessageAttachment>();
}
