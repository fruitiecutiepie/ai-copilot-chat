using ChatApp.Api.Services.Chat.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Chat.Db;

public class ChatDb : IChatDb
{
  private readonly ChatDbContext _db;
  public ChatDb(ChatDbContext db) => _db = db;

  public async Task SetChatMessage(ChatMessage msg)
  {
    _db.ChatMessages.Add(msg);
    await _db.SaveChangesAsync();
  }

  public async Task SetChatMessageAttachment(string messageId, string filePath)
  {
    _db.ChatDbs.Add(new ChatMessageAttachment
    {
      MessageId = messageId,
      FilePath = filePath
    });
    await _db.SaveChangesAsync();
  }

  public async Task<IReadOnlyList<ChatMessage>> GetChatMessages(
    string conversationId, int limit)
  {
    return await _db.ChatMessages
      .Where(m => m.ConvId == conversationId)
      .OrderBy(m => m.Timestamp)
      .Take(limit)
      .Include(m => m.Attachments)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<IEnumerable<string>> GetChatConversations(string userId)
  {
    return await _db.ChatMessages
      .Where(m => m.UserId == userId)
      .Select(m => m.ConvId)
      .Distinct()
      .ToListAsync();
  }
}