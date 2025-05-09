using ChatApp.Api.Data;
using ChatApp.Api.Hubs;
using Microsoft.EntityFrameworkCore;

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

app.MapControllers();
app.MapHub<ChatHub>("/hubs");

app.Run();
