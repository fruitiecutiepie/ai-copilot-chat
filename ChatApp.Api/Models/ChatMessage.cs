using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Models;

public class ChatMessage
{
  [Key]
  public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();

  [Required]
  public string UserId { get; set; } = default!;

  [Required]
  public string ConvId { get; set; } = NanoidDotNet.Nanoid.Generate();

  // [ForeignKey(nameof(UserId))]
  // public User User { get; set; } = default!;

  [Required]
  public string Content { get; set; } = default!;

  [Required]
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  [Required]
  public List<ChatMessageAttachment> Attachments { get; set; }
    = new List<ChatMessageAttachment>();
}
