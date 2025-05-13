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
    builder.Services.AddSingleton<SqliteConnection>(_ =>
    {

      var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
      conn.Open();
      conn.EnableExtensions(true);

      // https://github.com/asg017/sqlite-vec
      var extPath = Path.Combine(AppContext.BaseDirectory, "vec0.dylib");
      Console.WriteLine($"Loading vec0 from {extPath}");
      try
      {
        conn.LoadExtension(extPath);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Failed to load vec0: {ex.Message}");
        throw;
      }

      using var cmd0 = conn.CreateCommand();
      cmd0.CommandText = "SELECT vec_distance_cosine(x'00000000', x'00000000');";
      var zero = cmd0.ExecuteScalar(); // should return 0, no exception
      Console.WriteLine($"vec0 loaded: vector_distance test = {zero}");

      using (var cmd = conn.CreateCommand())
      {
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        // cmd.CommandText = @"
        //   DROP TABLE IF EXISTS doc_chunks;
        //   DROP TABLE IF EXISTS vec_doc_chunks;
        // ";
        // cmd.ExecuteNonQuery();

        cmd.CommandText = @"
          CREATE TABLE IF NOT EXISTS doc_chunks(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id TEXT NOT NULL,
            conv_id TEXT NOT NULL,
            chunk TEXT NOT NULL
          );
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
          CREATE VIRTUAL TABLE IF NOT EXISTS vec_doc_chunks
          USING vec0(
            doc_id INTEGER PRIMARY KEY,
            embedding FLOAT[1536] distance_metric=cosine
          );
        ";
        cmd.ExecuteNonQuery();
      }

      return conn;
    });
    // builder.Services.AddMemoryCache();

    // Services
    builder.Services.AddDbContext<DbServiceContext>((sp, opts) =>
    {
      var conn = sp.GetRequiredService<SqliteConnection>();
      opts.UseSqlite(conn);
    });
    builder.Services.AddScoped<IDbServiceContext, DbServiceContext>();
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

    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<LlmHub>("/hubs/llm");

    app.Run();
  }
}
