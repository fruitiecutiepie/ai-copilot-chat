using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatApp.Api.Services.Db;

public class ChatDbContext(
  DbContextOptions<ChatDbContext> opts
): DbContext(opts) {
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
  public DbSet<ChatMessageAttachment> ChatMessageAttachments => Set<ChatMessageAttachment>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ChatMessage>()
      .HasMany(m => m.Attachments)
      .WithOne(a => a.Message)
      .HasForeignKey(a => a.MessageId);

    modelBuilder.Entity<ChatMessageAttachment>()
      .HasIndex(a => a.FilePath)
      .IsUnique();
  }
}

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
  public ChatDbContext CreateDbContext(string[] args)
  {
    var baseDir = AppContext.BaseDirectory;
    var dbPath = Path.Combine(baseDir, "UserData", "chat.db");

    var options = new DbContextOptionsBuilder<ChatDbContext>()
      .UseSqlite($"Data Source={dbPath};Cache=Shared")
      .Options;

    return new ChatDbContext(options);
  }
}
