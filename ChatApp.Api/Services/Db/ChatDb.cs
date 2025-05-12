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

  public async Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k)
  {
    using var cmd = _vectorConn.CreateCommand();
    cmd.CommandText = @"
      SELECT c.chunk
        FROM vec_document_chunks AS v
        JOIN doc_chunks          AS c USING(id)
      ORDER BY v.embedding <-> $q
      LIMIT $k;
    ";
    cmd.Parameters.Add(new SqliteParameter("$q", SqliteType.Blob)
    { Value = MemoryMarshal.AsBytes<float>(query).ToArray() });
    cmd.Parameters.AddWithValue("$k", k);

    var results = new List<string>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
      results.Add(rdr.GetString(0));
    return results;
  }

  public async Task SetDocChunksAsync(string id, string chunk, float[] embedding)
  {
    using var tx = _vectorConn.BeginTransaction();

    var cmd1 = _vectorConn.CreateCommand();
    cmd1.CommandText = @"
      INSERT INTO doc_chunks(id, chunk)
        VALUES($id, $chunk)
      ON CONFLICT(id) DO UPDATE SET
        chunk = excluded.chunk;
    ";
    cmd1.Parameters.AddWithValue("$id", id);
    cmd1.Parameters.AddWithValue("$chunk", chunk);
    await cmd1.ExecuteNonQueryAsync();

    var cmd2 = _vectorConn.CreateCommand();
    cmd2.CommandText = @"
      INSERT INTO vec_document_chunks(id, embedding)
        VALUES($id, $vec)
      ON CONFLICT(id) DO UPDATE SET
        embedding = excluded.embedding;
    ";
    cmd2.Parameters.AddWithValue("$id", id);
    cmd2.Parameters.Add(new SqliteParameter("$vec", SqliteType.Blob) {
      Value = MemoryMarshal.AsBytes<float>(embedding).ToArray()
    });
    await cmd2.ExecuteNonQueryAsync();

    await tx.CommitAsync();
  }
}
