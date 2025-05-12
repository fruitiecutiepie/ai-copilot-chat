using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ChatApp.Api.Services.Chat;
using ChatApp.Api.Ports;
using ChatApp.Api.Services.Chat.Ui;
using ChatApp.Api.Services.Db;
using ChatApp.Api.Services.Db.Seed;

namespace ChatApp.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    var baseDir = AppContext.BaseDirectory;
    var dbPath  = Path.Combine(baseDir, "UserData", "chat.db");

    var builder = WebApplication.CreateBuilder(args);
    // Infrastructure
    builder.Services.AddDbContext<ChatDbContext>(opt =>
      opt.UseSqlite($"Data Source={dbPath};Cache=Shared")
    );
    builder.Services.AddSingleton<SqliteConnection>(_ =>
    {
      var c = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
      c.Open();
      using (var cmd = c.CreateCommand())
      {
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
          CREATE TABLE IF NOT EXISTS doc_chunks(
            id TEXT PRIMARY KEY,
            chunk TEXT
          );
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
          CREATE VIRTUAL TABLE IF NOT EXISTS vec_document_chunks
          USING vec0(
            id PRIMARY KEY,
            embedding FLOAT[1536] distance_metric=cosine
          );
        ";
        cmd.ExecuteNonQuery();
      }
      // https://github.com/asg017/sqlite-vec
      c.LoadExtension("vec0");
      return c;
    });
    builder.Services.AddSqliteVectorStore($"Data Source={dbPath};Cache=Shared");

    // builder.Services.AddMemoryCache();

    // Services
    builder.Services.AddScoped<IChatService, ChatService>();

    // CORS
    builder.Services.AddCors(opt => 
      opt.AddPolicy("AllowClient", p => {
        p.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
      })
    );

    // SignalR
    builder.Services.AddSignalR();

    // Controllers
    builder.Services.AddControllers().AddJsonOptions(opt =>
      opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    );

    var app = builder.Build();

    // Auto-migrate
    using (var scope = app.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
      db.Database.Migrate();

      await DbSeeder.SeedFromJsonAsync(db);
    }

    // Local file storage
    {
      var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "UserData/uploads");
      if (!Directory.Exists(uploadsPath))
      {
      Directory.CreateDirectory(uploadsPath);
      }

      app.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/api/uploads"
      });
    }

    app.UseCors("AllowClient");
    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");

    app.Run();
  }
}
