using System.Text.Json;
using ChatApp.Api.Data;
using ChatApp.Api.Data.Seeding;
using ChatApp.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(opt => opt.AddPolicy("AllowClient", p =>
{
  p.WithOrigins("http://localhost:3000")
   .AllowAnyHeader()
   .AllowAnyMethod()
   .AllowCredentials();
}));
builder.Services.AddDbContext<ChatDbContext>(opt =>
  opt.UseSqlite("Data Source=./data/chat.db"));

builder.Services.AddSignalR();
builder.Services.AddControllers()
  .AddJsonOptions(opt =>
  {
    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
  });

var app = builder.Build();

app.UseWebSockets();
app.UseCors("AllowClient");

{
  var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
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

app.MapControllers();
app.MapHub<ChatHub>("/hubs");

// auto-migrate
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
  db.Database.Migrate();
  
  await DbSeeder.SeedFromJsonAsync(db, "mockdata.json");
}

app.Run();
