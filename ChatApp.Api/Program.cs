using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

using ChatApp.Api.Services.Chat;
using ChatApp.Api.Services.Llm;
using ChatApp.Api.Ports;
using ChatApp.Api.Services.Chat.Ui;
using ChatApp.Api.Services.Llm.Ui;
using ChatApp.Api.Services.Db;
using ChatApp.Api.Services.Db.Seed;
using ChatApp.Api.Services.Fs;
using ChatApp.Api.Models;

namespace ChatApp.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    var baseDir = AppContext.BaseDirectory;
    var userData = Path.Combine(baseDir, "UserData");
    var dbPath = Path.Combine(userData, "chat.db");

    if (!Directory.Exists(userData))
      Directory.CreateDirectory(userData);

    var builder = WebApplication.CreateBuilder(args);

    // Config
    builder.Services.Configure<AppSettings>(builder.Configuration);

    // Infrastructure
    builder.Services.AddDbContext<DbServiceContext>(opt =>
      opt.UseSqlite($"Data Source={dbPath};Cache=Shared")
    );
    builder.Services.AddScoped<IDbServiceContext, DbServiceContext>();
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
    builder.Services.AddScoped<IDbService, DbService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<ILlmService, LlmService>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IFsService, FsService>();

    // HTTP Clients
    builder.Services.AddSingleton(sp => {
      var opts = sp.GetRequiredService<IOptions<AppSettings>>().Value;
      return new ChatClient(
        model: opts.OpenAI.Model, // "gpt-4.1-mini-2025-04-14" (https://platform.openai.com/docs/models/gpt-4.1-mini)
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

    app.UseCors("AllowClient");
    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<LlmHub>("/hubs/llm");

    app.Run();
  }
}
