using System.Text.Json;
using ChatApp.Api.Services.Chat;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Data.Seed;

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
        await db.Database.ExecuteSqlRawAsync(@"
          INSERT INTO ChatDbs (Id, MessageId, FilePath)
          VALUES ({0}, {1}, {2})
          ON CONFLICT(FilePath) DO NOTHING",
          NanoidDotNet.Nanoid.Generate(), s.Id, fp
        );
      }
    }
  }
}
