using ChatApp.Api.Data;
using ChatApp.Api.Dtos;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Hubs;

public class ChatHub(ChatDbContext db) : Hub
{
  private readonly ChatDbContext _db = db;

  /// <summary>
  /// Client calls this to start receiving messages for a conversation.
  /// </summary>
  public Task JoinConversation(
    string convId
  )
  {
    Console.WriteLine($"User {Context.UserIdentifier} connected to {convId}");
    return Groups.AddToGroupAsync(Context.ConnectionId, convId);
  }

  /// <summary>
  /// Client calls this when they leave a chat.
  /// </summary>
  public Task LeaveConversation(
    string convId
  )
  {
    Console.WriteLine($"User {Context.UserIdentifier} disconnected from {convId}");
    return Groups.RemoveFromGroupAsync(Context.ConnectionId, convId);
  }

  /// <summary>
  /// Send a new message to a conversation.
  /// </summary>
  public async Task SendMessage(
    string userId,
    string convId,
    string content,
    string attachmentsCsv
  ) {
    var msg = new ChatMessage
    {
      Id = NanoidDotNet.Nanoid.Generate(),
      UserId = userId,
      ConvId = convId,
      Content = content,
      Timestamp = DateTime.UtcNow,
      Attachments = attachmentsCsv
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(fp => new ChatMessageAttachment
        {
          FilePath = fp.Trim()
        })
        .ToList()
    };

    _db.ChatMessages.Add(msg);
    await _db.SaveChangesAsync();

    var dto = new ChatMessageDto(
      msg.Id,
      msg.UserId,
      msg.ConvId,
      msg.Content,
      msg.Timestamp.ToString("o"),
      msg.Attachments
        .Select(a => Path.GetFileName(a.FilePath))
        .ToArray()
    );

    await Clients.Group(convId).SendAsync("ReceiveMessage", dto);
  }

  /// <summary>
  /// Load a page of past messages for infinite‐scroll or history view.
  /// </summary>
  public async Task<IEnumerable<object>> GetHistory(
    string convId,
    int pageNumber = 1,
    int pageSize = 50
  )
  {
    var msgs = await _db.ChatMessages
    .Where(m => m.ConvId == convId)
    .OrderByDescending(m => m.Timestamp)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Include(m => m.Attachments)
    .AsSplitQuery() // prevents join duplicates by running separate queries
    .AsNoTracking()
    .ToListAsync();

    var ctx = Context.GetHttpContext()
      ?? throw new InvalidOperationException("Context is null");
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";

    var res = msgs.Select(m => new ChatMessageDto(
      m.Id,
      m.UserId,
      m.ConvId,
      m.Content,
      m.Timestamp.ToString("o"),
      m.Attachments
        .Select(att => $"{baseUrl}/uploads/{Path.GetFileName(att.FilePath)}")
        .ToArray()
    ));
    return res;
  }
}
