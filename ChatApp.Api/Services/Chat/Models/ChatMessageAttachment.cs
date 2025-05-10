using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.Api.Services.Chat.Models;

public class ChatMessageAttachment
{
  [Key]
  public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();

  [Required]
  public string MessageId { get; set; } = default!;

  [ForeignKey(nameof(MessageId))]
  public ChatMessage Message { get; set; } = default!;

  [Required]
  public string FilePath { get; set; } = default!;
}
