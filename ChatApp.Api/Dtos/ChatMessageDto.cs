namespace ChatApp.Api.Dtos;

public record ChatMessageDto(
  string Id,
  string UserId,
  string ConvId,
  string Content,
  string Timestamp,
  string[] AttachmentUrls
);