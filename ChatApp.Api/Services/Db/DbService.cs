using System.Runtime.InteropServices;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Db;

public class DbService : IDbService
{
  private static readonly SemaphoreSlim _semaphore = new(1,1);
  private readonly IDbServiceContext _dbContext;
  private readonly SqliteConnection _vectorConn;

  public DbService(
    IDbServiceContext dbContext,
    SqliteConnection vectorConn
  )
  {
    _dbContext = dbContext;
    _vectorConn = vectorConn;
  }

  public async Task SetDbChatMessagesAsync(
    IEnumerable<ChatMessage> messages
  ) {
    _dbContext.ChatMessages.AddRange(messages);
    await _dbContext.SaveChangesAsync();
  }

  public async Task SetDbChatMessageAttachmentsAsync(
    IEnumerable<ChatMessageAttachment> attachments
  ) {
    _dbContext.ChatMessageAttachments.AddRange(attachments);
    await _dbContext.SaveChangesAsync();
  }

  public async Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(
    string convId
  ) {
    return await _dbContext.ChatMessages
      .Where(m => m.ConvId == convId)
      .OrderBy(m => m.Timestamp)
      .Include(m => m.Attachments)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<IEnumerable<string>> GetDbChatConversationsAsync(string userId)
  {
    return await _dbContext.ChatMessages
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
        FROM vec_doc_chunks AS v
        JOIN doc_chunks          AS c
          ON v.doc_id = c.id
       ORDER BY vec_distance_cosine(v.embedding, $q)
       LIMIT $k;
    ";
    // pass raw float32 bytes for the vector parameter
    cmd.Parameters.Add(new SqliteParameter("$q", SqliteType.Blob)
    {
      Value = MemoryMarshal.AsBytes<float>(query).ToArray()
    });
    cmd.Parameters.AddWithValue("$k", k);

    var results = new List<string>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
      results.Add(rdr.GetString(0));
    return results;
  }

  public async Task SetDocChunksAsync(
    string userId,
    string convId,
    string chunk,
    float[] embedding
  ) {
    await _semaphore.WaitAsync();
    try {
      using var tx = _vectorConn.BeginTransaction();

      var cmd1 = _vectorConn.CreateCommand();
      cmd1.CommandText = @"
        INSERT INTO doc_chunks(user_id, conv_id, chunk)
        VALUES($user_id, $conv_id, $chunk);
      ";
      cmd1.Parameters.AddWithValue("$user_id", userId);
      cmd1.Parameters.AddWithValue("$conv_id", convId);
      cmd1.Parameters.AddWithValue("$chunk", chunk);
      await cmd1.ExecuteNonQueryAsync();

      var lastIdCmd = _vectorConn.CreateCommand();
      lastIdCmd.CommandText = "SELECT last_insert_rowid();";
      var docId = (long)await lastIdCmd.ExecuteScalarAsync();

      var cmd2 = _vectorConn.CreateCommand();
      cmd2.CommandText = @"
        INSERT OR REPLACE INTO vec_doc_chunks(doc_id, embedding)
        VALUES($doc_id, $vec);
      ";
      cmd2.Parameters.AddWithValue("$doc_id", docId);
      cmd2.Parameters.Add(new SqliteParameter("$vec", SqliteType.Blob) {
        Value = MemoryMarshal.AsBytes<float>(embedding).ToArray()
      });
      await cmd2.ExecuteNonQueryAsync();

      await tx.CommitAsync();
    }
    finally {
      _semaphore.Release();
    }
  }
}
