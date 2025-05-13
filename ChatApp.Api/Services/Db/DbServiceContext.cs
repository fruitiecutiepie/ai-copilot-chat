using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatApp.Api.Services.Db;

public class DbServiceContext : DbContext, IDbServiceContext
{
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
  public DbSet<ChatMessageAttachment> ChatMessageAttachments => Set<ChatMessageAttachment>();

  public DbServiceContext(
    DbContextOptions<DbServiceContext> opts
  ) : base(opts) { }

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

public class DbServiceContextFactory : IDesignTimeDbContextFactory<DbServiceContext>
{
  public DbServiceContext CreateDbContext(string[] args)
  {
    var baseDir = AppContext.BaseDirectory;
    var dbPath = Path.Combine(baseDir, "UserData", "chat.db");

    var options = new DbContextOptionsBuilder<DbServiceContext>()
      .UseSqlite($"Data Source={dbPath};Cache=Shared")
      .Options;

    return new DbServiceContext(options);
  }
}
