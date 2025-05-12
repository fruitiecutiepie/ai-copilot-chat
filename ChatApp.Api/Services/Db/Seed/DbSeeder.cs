using System.Text.Json;
using ChatApp.Api.Services.Db;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Db.Seed;

public static class DbSeeder
{
  private const string jsonPath = "Data/Seed/seed.json";

  public class ChatMessageSeed
  {
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string ConvId { get; set; }
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public required List<string> Attachments { get; set; }
  }

  public static async Task SeedFromJsonAsync(
    ChatDbContext db
  )
  {
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
        string fileName = Path.GetFileName(fp);
        string fileType = Path.GetExtension(fp);
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
