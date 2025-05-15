using System.Text.Json.Serialization;

namespace ChatApp.Api.Models;

public class ChatMessageAttachment
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum AttachmentType
  {
    Text,
    Json,
    Csv,
    Pdf,
    Image,
    Other
  }

  public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();
  public string MessageId { get; set; } = default!;
  public string FileName { get; set; } = default!;
  public AttachmentType FileType { get; set; } = default!;
  public string FilePath { get; set; } = default!;
}
