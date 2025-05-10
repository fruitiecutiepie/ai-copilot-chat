using ChatApp.Api.Services.Chat.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Chat;

public class ChatDbContext(
  DbContextOptions<ChatDbContext> opts
): DbContext(opts) {
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
  public DbSet<ChatMessageAttachment> ChatDbs => Set<ChatMessageAttachment>();

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
