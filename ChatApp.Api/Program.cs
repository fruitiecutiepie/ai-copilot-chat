using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

using ChatApp.Api.Services.Chat;
using ChatApp.Api.Services.Llm;
using ChatApp.Api.Ports;
using ChatApp.Api.Services.Llm.Ui;
using ChatApp.Api.Services.Db;
using ChatApp.Api.Models;
using Dapper;
using System.Data;

namespace ChatApp.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    SQLitePCL.Batteries_V2.Init();

    var userDataPath = "./UserData";
    var dbPath = Path.Combine(userDataPath, "chat.db");

    if (!Directory.Exists(userDataPath))
      Directory.CreateDirectory(userDataPath);

    AppContext.SetSwitch("Microsoft.Data.Sqlite.EnableExtensions", true);

    var builder = WebApplication.CreateBuilder(args);

    // Config
    builder.Services.Configure<AppSettings>(builder.Configuration);

    // Infrastructure
    builder.Services.AddSingleton<IDbConnection>(_ =>
    {
      var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
      conn.Open();
      conn.EnableExtensions(true);

      var extPath = Path.Combine(AppContext.BaseDirectory, "vec0.dylib");
      conn.LoadExtension(extPath);

      // sanity check
      var zero = conn.ExecuteScalar<long>(
        "SELECT vec_distance_cosine(x'00000000', x'00000000');"
      );
      Console.WriteLine($"vec0 loaded: {zero}");

      // ensure foreign keys and WAL mode
      conn.Execute("PRAGMA foreign_keys = ON;");
      conn.Execute("PRAGMA journal_mode = WAL;");
      conn.Execute("PRAGMA synchronous = NORMAL;");

      // vector‐db tables
      conn.Execute(@"
        CREATE TABLE IF NOT EXISTS doc_chunks(
          id         INTEGER PRIMARY KEY,
          sender_id  TEXT    NOT NULL,
          conv_id    TEXT    NOT NULL,
          chunk      TEXT    NOT NULL
        );
      ");
      conn.Execute(@"
        CREATE VIRTUAL TABLE IF NOT EXISTS vec_doc_chunks
        USING vec0(
          doc_id     INTEGER PRIMARY KEY REFERENCES doc_chunks(id) ON DELETE CASCADE,
          embedding  FLOAT[1536]  distance_metric=cosine
        );
      ");

      // ChatMessages table
      conn.Execute(@"
        CREATE TABLE IF NOT EXISTS ChatMessages (
          Id         TEXT    PRIMARY KEY,
          ConvId     TEXT    NOT NULL,
          SenderId   TEXT    NOT NULL,
          ReceiverId TEXT    NOT NULL,
          Content    TEXT,
          Timestamp  TEXT    NOT NULL
        );
      ");
      // index for all queries by conv, role, user, time
      conn.Execute(@"
        CREATE INDEX IF NOT EXISTS
          IX_ChatMessages_ConvRoleUserTime
        ON ChatMessages(ConvId, SenderId, ReceiverId, Timestamp);
      ");

      // conv / timestamp index for any full-stream scan
      conn.Execute(@"
        CREATE INDEX IF NOT EXISTS
          IX_ChatMessages_ConvTime
        ON ChatMessages(ConvId, Timestamp);
      ");

      // ChatMessageAttachments table
      conn.Execute(@"
        CREATE TABLE IF NOT EXISTS ChatMessageAttachments (
          Id         TEXT    PRIMARY KEY,
          FilePath   TEXT    NOT NULL UNIQUE,
          MessageId  TEXT    NOT NULL,
          FileName   TEXT    NOT NULL DEFAULT '',
          FileType   INTEGER NOT NULL DEFAULT 0,
          FOREIGN KEY(MessageId) REFERENCES ChatMessages(Id) ON DELETE CASCADE
        );
      ");
      conn.Execute(@"
        CREATE INDEX IF NOT EXISTS
          IX_ChatMessageAttachments_MessageId
        ON ChatMessageAttachments(MessageId);
      ");

      return conn;
    });

    // Services
    builder.Services.AddScoped<IDbService, DbService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<ILlmService, LlmService>();

    builder.Services.AddHttpContextAccessor();

    // HTTP Clients
    builder.Services.AddSingleton(sp => {
      var opts = sp.GetRequiredService<IOptions<AppSettings>>().Value;
      return new ChatClient(
        // https://platform.openai.com/docs/models/gpt-4.1-mini
        model: opts.OpenAI.Model, // "gpt-4.1-mini-2025-04-14"
        apiKey: opts.OpenAI.ApiKey
      );
    });
    builder.Services.AddHttpClient("Cohere", (sp, client) =>
    {
      var config = sp.GetRequiredService<IOptions<AppSettings>>().Value;
      client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", config.Cohere.ApiKey);
    });

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

    app.UseRouting();
    app.UseCors("AllowClient");

    // Local file storage
    {
      var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "UserData/uploads");
      if (!Directory.Exists(uploadsPath))
        Directory.CreateDirectory(uploadsPath);

      app.UseStaticFiles(new StaticFileOptions {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
      });
    }

    app.MapControllers();
    // app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<LlmHub>("/hubs/llm");

    // Auto-migrate
    using (var scope = app.Services.CreateScope())
    {
      var conn = scope.ServiceProvider.GetRequiredService<IDbConnection>();

      // check if we already have any messages
      var existing = await conn.QuerySingleAsync<int>(
        "SELECT COUNT(1) FROM ChatMessages"
      );
      if (existing == 0)
      {
        var chatSvc = scope.ServiceProvider.GetRequiredService<IChatService>();

        var seedJson = File.ReadAllText(Path.Combine(app.Environment.ContentRootPath, "seed.json"));
        var seeds = JsonSerializer.Deserialize<List<ChatMessagesSeed>>(seedJson,
          new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (seeds != null)
        {
          Console.WriteLine($"Seeding {seeds.Count} chat messages...");
          foreach (var s in seeds)
          {
            var msg = new Models.ChatMessage
            {
              Id = s.Id,
              SenderId = s.SenderId,
              ReceiverId = s.ReceiverId,
              ConvId = s.ConvId,
              Content = s.Content,
              Timestamp = s.Timestamp,
              Attachments = s.Attachments.Select(fp =>
              {
                var fileName = Path.GetFileName(fp);
                var ext = Path.GetExtension(fp).ToLowerInvariant().TrimStart('.');
                var type = ext switch
                {
                  "txt" or "md" => ChatMessageAttachment.AttachmentType.Text,
                  "json" => ChatMessageAttachment.AttachmentType.Json,
                  "csv" => ChatMessageAttachment.AttachmentType.Csv,
                  "pdf" => ChatMessageAttachment.AttachmentType.Pdf,
                  "png" or "jpg" or "jpeg" => ChatMessageAttachment.AttachmentType.Image,
                  _ => ChatMessageAttachment.AttachmentType.Other
                };

                return new ChatMessageAttachment
                {
                  Id = NanoidDotNet.Nanoid.Generate(),
                  MessageId = s.Id,
                  FileName = fileName,
                  FilePath = fileName,
                  FileType = type
                };
              }).ToList()
            };

            await chatSvc.SetChatMessageAsync(msg);
          }
        }
      }
    }

    app.Run();
  }
}
