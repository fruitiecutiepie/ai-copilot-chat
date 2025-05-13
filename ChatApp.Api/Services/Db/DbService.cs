using System.Data;
using System.Runtime.InteropServices;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services.Db;

public class ChatMessageSeed
{
  public required string Id { get; set; }
  public required string UserId { get; set; }
  public required string ConvId { get; set; }
  public required string Content { get; set; }
  public required DateTime Timestamp { get; set; }
  public required List<string> Attachments { get; set; }
}

public class DbService : IDbService
{
  private static readonly SemaphoreSlim _semaphore = new(1, 1);
  private readonly IDbConnection _db;

  public DbService(
    IDbConnection dbContext
  )
  {
    _db = dbContext;
  }

  public Task SetDbChatMessages(
    IEnumerable<ChatMessage> messages
  )
  {
    const string sql = @"
      INSERT OR IGNORE INTO ChatMessages
        (Id, Content, ConvId, Timestamp, UserId)
      VALUES
        (@Id, @Content, @ConvId, @Timestamp, @UserId)";
    return _db.ExecuteAsync(sql, messages);
  }

  public Task SetDbChatMessageAttachments(
    IEnumerable<ChatMessageAttachment> attachments
  )
  {
    const string sql = @"
      INSERT OR IGNORE INTO ChatMessageAttachments
        (Id, FilePath, MessageId, FileName, FileType)
      VALUES
        (@Id, @FilePath, @MessageId, @FileName, @FileType)";
    return _db.ExecuteAsync(sql, attachments);
  }

  public async Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(
    string convId
  )
  {
    const string sql = @"
      SELECT
        m.Id, m.Content, m.ConvId, m.Timestamp, m.UserId,
        a.Id, a.FilePath, a.MessageId, a.FileName, a.FileType
      FROM ChatMessages m
      LEFT JOIN ChatMessageAttachments a
        ON a.MessageId = m.Id
      WHERE m.ConvId = @ConvId
      ORDER BY m.Timestamp";
    var lookup = new Dictionary<string, ChatMessage>();
    await _db.QueryAsync<ChatMessage, ChatMessageAttachment, ChatMessage>(
      sql,
      (msg, att) =>
      {
        if (!lookup.TryGetValue(msg.Id, out var entry))
        {
          entry = msg;
          entry.Attachments = new List<ChatMessageAttachment>();
          lookup[msg.Id] = entry;
        }
        if (att != null)
          entry.Attachments.Add(att);
        return entry;
      },
      new { ConvId = convId },
      splitOn: "Id"
    );
    return lookup.Values.ToList();
  }

  public Task<IEnumerable<string>> GetDbChatConversations(string userId)
  {
    const string sql = @"
      SELECT DISTINCT ConvId
      FROM ChatMessages
      WHERE UserId = @UserId";
    return _db.QueryAsync<string>(sql, new { UserId = userId });
  }

  public async Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k)
  {
    const string sql = @"
      SELECT c.chunk
        FROM vec_doc_chunks AS v
        JOIN doc_chunks          AS c
          ON v.doc_id = c.id
       ORDER BY vec_distance_cosine(v.embedding, @q)
       LIMIT @k;
    ";
    var qBytes = MemoryMarshal.AsBytes<float>(query).ToArray();
    var rows = await _db.QueryAsync<string>(sql, new { q = qBytes, k });
    return rows.ToList();
  }

  public async Task SetDocChunksAsync(
    string userId,
    string convId,
    string chunk,
    float[] embedding
  )
  {
    await _semaphore.WaitAsync();
    try
    {
      using var tx = _db.BeginTransaction();

      // 1) insert doc_chunks and get new id
      const string insertDoc = @"
        INSERT INTO doc_chunks(user_id, conv_id, chunk)
        VALUES(@userId, @convId, @chunk);
        SELECT last_insert_rowid();
      ";
      var docId = await _db.ExecuteScalarAsync<long>(
        insertDoc,
        new { userId, convId, chunk },
        tx
      );

      // 2) upsert into vec_doc_chunks
      const string upsertVec = @"
        INSERT OR REPLACE INTO vec_doc_chunks(doc_id, embedding)
        VALUES(@docId, @vec);
      ";
      var vecBytes = MemoryMarshal.AsBytes<float>(embedding).ToArray();
      await _db.ExecuteAsync(
        upsertVec,
        new { docId, vec = vecBytes },
        tx
      );

      tx.Commit();
    }
    finally
    {
      _semaphore.Release();
    }
  }
}
