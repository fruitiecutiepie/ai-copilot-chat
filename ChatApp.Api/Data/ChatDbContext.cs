using Microsoft.EntityFrameworkCore;
using ChatApp.Api.Models;

namespace ChatApp.Api.Data;

public class ChatDbContext(
  DbContextOptions<ChatDbContext> opts
): DbContext(opts) {
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ChatMessage>()
      .HasMany(m => m.Attachments)
      .WithOne(a => a.Content)
      .HasForeignKey(a => a.MessageId);
  }
}
