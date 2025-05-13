using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChatApp.Api.Models;

public class ChatMessageAttachment
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum AttachmentType
  {
    Image,
    Pdf
  }

  [Key]
  public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();

  [Required]
  public string MessageId { get; set; } = default!;

  [JsonIgnore]
  [ForeignKey(nameof(MessageId))]
  public ChatMessage Message { get; set; } = default!;

  [Required]
  public string FileName { get; set; } = default!;

  [Required]
  public AttachmentType FileType { get; set; } = default!;

  [Required]
  public string FilePath { get; set; } = default!;
}
