using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

using ChatApp.Api.Data.Seed;
using ChatApp.Api.Services.Chat;
using ChatApp.Api.Services.Chat.Ui.Ws;
using ChatApp.Api.Services.FileStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(opt =>
  opt.UseSqlite("Data Source=./Data/chat.db")
);

builder.Services.AddSignalR();

builder.Services.AddControllers().AddJsonOptions(opt =>
  opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
);
builder.Services.AddCors(opt => opt.AddPolicy("AllowClient", p =>
{
  p.WithOrigins("http://localhost:3000")
   .AllowAnyHeader()
   .AllowAnyMethod()
   .AllowCredentials();
}));


builder.Services
  .AddScoped<IChatService, ChatService>()
  .AddScoped<IFileStorage, FileStorage>();

var app = builder.Build();

// auto-migrate
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
  db.Database.Migrate();

  await DbSeeder.SeedFromJsonAsync(db);
}

{
  var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Data/uploads");
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

app.UseWebSockets();
app.UseCors("AllowClient");

app.MapControllers();
app.MapHub<ChatWs>("/ws/chat");

app.Run();
