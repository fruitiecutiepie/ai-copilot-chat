using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Data.Seeding;

public static class DbSeeder
{
  public static async Task SeedFromJsonAsync(
    ChatDbContext db,
    string jsonPath
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
        await db.Database.ExecuteSqlRawAsync(@"
          INSERT INTO ChatMessageAttachments (Id, MessageId, FilePath)
          VALUES ({0}, {1}, {2})
          ON CONFLICT(FilePath) DO NOTHING",
          NanoidDotNet.Nanoid.Generate(), s.Id, fp
        );
      }
    }
  }
}
