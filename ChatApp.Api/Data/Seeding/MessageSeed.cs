namespace ChatApp.Api.Data.Seeding;

public class ChatMessageSeed
{
  public required string Id { get; set; }
  public required string UserId { get; set; }
  public required string ConvId { get; set; }
  public required string Content { get; set; }
  public DateTime Timestamp { get; set; }
  public required List<string> Attachments { get; set; }
}