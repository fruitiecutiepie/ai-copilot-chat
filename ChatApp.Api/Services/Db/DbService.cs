using System.Data;
using System.Runtime.InteropServices;
using ChatApp.Api.Models;
using ChatApp.Api.Ports;
using Dapper;

namespace ChatApp.Api.Services.Db;

public class ChatMessagesSeed
{
  public required string Id { get; set; }
  public required string SenderId { get; set; }
  public required string ReceiverId { get; set; }
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
  ) {
    _db = dbContext;
  }

  public async Task<IReadOnlyList<ChatMessage>> GetDbChatMessagesAsync(
    string convId
  ) {
    const string sql = @"
      SELECT
        m.Id, m.ConvId, m.SenderId, m.ReceiverId, m.Content, m.Timestamp,
        a.Id, a.FilePath, a.MessageId, a.FileName, a.FileType
      FROM ChatMessages m
      LEFT JOIN ChatMessageAttachments a
        ON a.MessageId = m.Id
      WHERE m.ConvId = @convId
      ORDER BY m.Timestamp
    ";
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
      new { convId },
      splitOn: "Id"
    );
    return lookup.Values.ToList();
  }

  public async Task SetDbChatMessagesWithAttachments(IEnumerable<ChatMessage> messages)
  {
    const string msgSql = @"
      INSERT OR REPLACE INTO ChatMessages
        (Id, ConvId, SenderId, ReceiverId, Content, Timestamp)
      VALUES
        (@Id, @ConvId, @SenderId, @ReceiverId, @Content, @Timestamp);
    ";
    const string attSql = @"
      INSERT OR REPLACE INTO ChatMessageAttachments
        (Id, FilePath, MessageId, FileName, FileType)
      VALUES
        (@Id, @FilePath, @MessageId, @FileName, @FileType);
    ";

    using var tx = _db.BeginTransaction();
    foreach (var msg in messages)
    {
      await _db.ExecuteAsync(msgSql, new[] { msg }, tx);

      if (msg.Attachments?.Count > 0)
        await _db.ExecuteAsync(attSql, msg.Attachments, tx);
    }
    tx.Commit();
  }

  public async Task<List<string>> GetDocChunksContentTopKAsync(float[] query, int k)
  {
    const string sql = @"
      SELECT c.chunk
        FROM vec_doc_chunks AS v
        JOIN doc_chunks     AS c
          ON v.doc_id = c.id
       ORDER BY vec_distance_cosine(v.embedding, @q)
       LIMIT @k;
    ";
    var qBytes = MemoryMarshal.AsBytes<float>(query).ToArray();
    var rows = await _db.QueryAsync<string>(sql, new { q = qBytes, k });
    return rows.ToList();
  }

  public async Task SetDocChunksAsync(
    string senderId,
    string convId,
    string chunk,
    float[] embedding
  ) {
    await _semaphore.WaitAsync();
    try
    {
      using var tx = _db.BeginTransaction();

      string stringId = NanoidDotNet.Nanoid.Generate(alphabet: "0123456789", size: 9);
      if (!int.TryParse(stringId, out var id))
        throw new InvalidOperationException(
          $"Cannot convert NanoID '{stringId}' to Int32."
        );

      const string insertDoc = @"
        INSERT OR REPLACE INTO doc_chunks(id, sender_id, conv_id, chunk)
        VALUES(@id, @senderId, @convId, @chunk);
      ";
      await _db.ExecuteAsync(
        insertDoc,
        new { id, senderId, convId, chunk },
        tx
      );

      const string upsertVec = @"
        INSERT OR REPLACE INTO vec_doc_chunks(doc_id, embedding)
        VALUES(@docId, @vec);
      ";
      var vecBytes = MemoryMarshal.AsBytes<float>(embedding).ToArray();
      await _db.ExecuteAsync(
        upsertVec,
        new { docId = id, vec = vecBytes },
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
