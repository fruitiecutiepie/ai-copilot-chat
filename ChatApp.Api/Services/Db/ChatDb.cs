using System.Runtime.InteropServices;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Db;

public class ChatDb : IChatDb
{
  private readonly ChatDbContext _db;
  private readonly SqliteConnection _vectorConn;

  public ChatDb(
    ChatDbContext db,
    SqliteConnection vectorConn
  )
  {
    _db = db;
    _vectorConn = vectorConn;
  }

  public async Task SetDbChatMessageAsync(ChatMessage msg)
  {
    _db.ChatMessages.Add(msg);
    await _db.SaveChangesAsync();
  }

  public async Task SetDbChatMessageAttachmentAsync(string messageId, string filePath)
  {
    _db.ChatMessageAttachments.Add(new ChatMessageAttachment
    {
      MessageId = messageId,
      FilePath = filePath
    });
    await _db.SaveChangesAsync();
  }

  public async Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(
    string convId, int limit)
  {
    return await _db.ChatMessages
      .Where(m => m.ConvId == convId)
      .OrderBy(m => m.Timestamp)
      .Take(limit)
      .Include(m => m.Attachments)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<IEnumerable<string>> GetDbChatConversationsAsync(string userId)
  {
    return await _db.ChatMessages
      .Where(m => m.UserId == userId)
      .Select(m => m.ConvId)
      .Distinct()
      .ToListAsync();
  }
}
