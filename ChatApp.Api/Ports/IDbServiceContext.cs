using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Ports;

public interface IDbServiceContext
{
  DbSet<ChatMessage> ChatMessages { get; }
  DbSet<ChatMessageAttachment> ChatMessageAttachments { get; }

  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
