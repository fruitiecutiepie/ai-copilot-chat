using System.Text.Json;
using ChatApp.Api.Models;
using ChatApp.Api.Services.Db;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Db.Seed;

public static class DbSeeder
{
  private const string jsonPath = "Services/Db/Seed/seed.json";

  public class ChatMessageSeed
  {
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string ConvId { get; set; }
    public required string Content { get; set; }
    public required DateTime Timestamp { get; set; }
    public required List<string> Attachments { get; set; }
  }

  public static async Task SeedFromJsonAsync(
    DbServiceContext db
  ) {
    var json = File.ReadAllText(jsonPath);
    var seeds = JsonSerializer.Deserialize<List<ChatMessageSeed>>(json,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    if (seeds == null || seeds.Count == 0)
      return;

    foreach (var s in seeds)
    {
      await db.Database.ExecuteSqlRawAsync(@"
        INSERT INTO ChatMessages (Id, UserId, ConvId, Content, Timestamp)
        VALUES ({0}, {1}, {2}, {3}, {4})
        ON CONFLICT(Id) DO NOTHING",
        s.Id, s.UserId, s.ConvId, s.Content, s.Timestamp
      );

      foreach (var fp in s.Attachments)
      {
        // Example: "../uploads/specs_comparison.pdf"
        string fileName = Path.GetFileName(fp); // "specs_comparison.pdf"
        string fileExt = Path.GetExtension(fp); // ".pdf"

        string fileType = ChatMessageAttachment.AttachmentType.Other.ToString();
        switch (fileExt)
        {
          case ".txt":
          case ".md":
            fileType = ChatMessageAttachment.AttachmentType.Text.ToString();
            break;
          case ".json":
            fileType = ChatMessageAttachment.AttachmentType.Json.ToString();
            break;
          case ".csv":
            fileType = ChatMessageAttachment.AttachmentType.Csv.ToString();
            break;
          case ".pdf":
            fileType = ChatMessageAttachment.AttachmentType.Pdf.ToString();
            break;
          case ".png":
          case ".jpg":
          case ".jpeg":
            fileType = ChatMessageAttachment.AttachmentType.Image.ToString();
            break;
          default:
            Console.WriteLine($"Unknown file type: {fileType}");
            break;
        }
        await db.Database.ExecuteSqlRawAsync(@"
          INSERT INTO ChatMessageAttachments (Id, MessageId, FileName, FilePath, FileType)
          VALUES ({0}, {1}, {2}, {3}, {4})
          ON CONFLICT(FilePath) DO NOTHING",
          NanoidDotNet.Nanoid.Generate(), s.Id, fileName, fp, fileType
        );
      }
    }
  }
}
