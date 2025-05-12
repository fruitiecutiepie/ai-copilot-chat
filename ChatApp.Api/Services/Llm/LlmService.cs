// using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Net.Http;
using PDFtoImage;
using SkiaSharp;
using System.Net.Http.Json;
using ChatApp.Api.Ports;
using OpenAI.Chat;
using System.Runtime.Versioning;

namespace ChatApp.Api.Services.Llm;

public enum EmbeddingInputType
{
  Query,
  Text,
  Image,
  Pdf
}

public class LlmService : ILlmService
{
  readonly IChatDb _db;

  private readonly ChatClient _clientOpenAi;
  private readonly HttpClient _httpCohere;

  // private readonly IMemoryCache _cache;
  // private readonly ConcurrentDictionary<string, Task<string>> _atchSummaryCache = new();

  public LlmService(
    IChatDb db,

    ChatClient chatClientOpenAi,
    IHttpClientFactory httpFactory
  // IMemoryCache cache
  )
  {
    _db = db;

    _clientOpenAi = chatClientOpenAi;
    _httpCohere = httpFactory.CreateClient("Cohere");

    // _cache = cache;
  }

  public async IAsyncEnumerable<string> StreamCompletionAsync(
    string convId,
    string userId,
    string content
  )
  {
    // assemble system + RAG multimodal + history summary
    var messages = await GetPromptMessagesAsync(convId, userId, content);

    await foreach (var update in _clientOpenAi.CompleteChatStreamingAsync(messages))
    {
      var text = update.ContentUpdate.FirstOrDefault()?.Text;
      if (!string.IsNullOrEmpty(text))
        yield return text;
    }
  }

  public async Task<List<OpenAI.Chat.ChatMessage>> GetPromptMessagesAsync(
    string convId,
    string userId,
    string content
  )
  {
    var msgs = new List<OpenAI.Chat.ChatMessage>{
      new SystemChatMessage(
        "You are a concise, factual AI assistant. Cite sources and warn if you’re speculating."
      )
    };

    var embeddings = await GetEmbeddingAsync(EmbeddingInputType.Query, content);
    var queryVector = embeddings.First();

    var docs = await _db.GetDocChunksContentTopKAsync(queryVector, 5);
    if (docs.Any())
    {
      msgs.Add(new SystemChatMessage(
        $"Here are relevant excerpts: {string.Join("\n", docs.ToArray())}"
      ));
    }

    // var attachments = history.SelectMany(m => m.Attachments).ToList();
    // var atchSummary = attachments
    //   .Select(att => _atchSummaryCache.GetOrAdd(att.Id, _ => GetAttachmentSummaryAsync(att)))
    //   .ToList();

    // // Wait for all summaries to complete
    // var summaries = await Task.WhenAll(atchSummary);
    // for (int i = 0; i < attachments.Count; i++)
    // {
    //   var sum = summaries[i];
    //   if (!string.IsNullOrEmpty(sum))
    //     msgs.Add(new {
    //       ChatMessageRole.System,
    //       new ChatMessageContent($"Attachment '{attachments[i].FileName}' summary:\n{sum}")
    //     });
    // }

    // var history = await _db.GetDbChatMessagesAsync(convId, 15);
    // var historySummary = await GetTextSummary(
    //   string.Join("\n", history.Select(m => m.Content))
    // );
    // msgs.Add(new SystemChatMessage(
    //   $"Here is the conversation history:\n{historySummary}"
    // ));

    return msgs;
  }

  public async Task<List<float[]>> GetEmbeddingAsync(EmbeddingInputType type, string source)
  {
    object[] inputs;
    switch (type)
    {
      case EmbeddingInputType.Query:
      case EmbeddingInputType.Text:
        // Max tokens (context length): https://docs.cohere.com/docs/cohere-embed
        var textChunks = SplitByTokens(source, 128_000);
        inputs = textChunks.Select(chunk => new
        {
          content = new object[] {
            new { type = "text", text = chunk }
          }
        }).ToArray();
        break;

      case EmbeddingInputType.Image:
        var imgBytes = await File.ReadAllBytesAsync(source);
        var ext = Path.GetExtension(source).TrimStart('.');
        var b64 = Convert.ToBase64String(imgBytes);

        var imageChunks = SplitByTokens(b64, 128_000);
        inputs = imageChunks.Select(chunk => new
        {
          content = new object[] {
            new { type = "text", text = source },
            new {
              type = "image_url",
              image_url = new { url = $"data:image/{ext};base64,{chunk}" }
            }
          }
        }).ToArray();
        break;

      case EmbeddingInputType.Pdf:
        var pagesB64 = RenderPdfToBase64Pages(source);
        var pdfChunks = SplitByTokens(string.Concat(pagesB64), 128_000);
        inputs = pdfChunks.Select(chunk => new
        {
          content = new object[] {
            new { type = "text", text = source },
            new {
              type = "image_url",
              image_url = new { url = $"data:image/png;base64,{chunk}" }
            }
          }
        }).ToArray();
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    var reqBody = new
    {
      model = "embed-v4.0",
      input_type = type == EmbeddingInputType.Query
        ? "search_query"
        : "search_document",
      embedding_types = new[] { "float" },
      inputs
    };

    var resp = await _httpCohere.PostAsJsonAsync(
      "https://api.cohere.com/v2/embed",
      reqBody
    );
    resp.EnsureSuccessStatusCode();

    using var doc = JsonDocument.Parse(
      await resp.Content.ReadAsStringAsync()
    );
    var floatArrays = doc.RootElement
      .GetProperty("embeddings")
      .GetProperty("float")
      .EnumerateArray();

    var results = new List<float[]>();
    foreach (var arr in floatArrays)
    {
      results.Add(arr
        .EnumerateArray()
        .Select(e => e.GetSingle())
        .ToArray()
      );
    }
    return results;
  }

  private static List<string> SplitByTokens(
    string text,
    int maxTokens
  )
  {
    if (maxTokens <= 0)
      throw new ArgumentOutOfRangeException(
        nameof(maxTokens), "maxTokens must be > 0."
      );

    var encoder = new Tiktoken.Encoder(new Tiktoken.Encodings.O200KBase());
    var tokens = encoder.Encode(text).ToList();

    var total = tokens is ICollection<int> c
      ? c.Count
      : tokens.Count();

    if (total <= maxTokens)
      return new List<string> { text };

    var chunks = new List<string>();
    for (int i = 0; i < tokens.Count; i += maxTokens)
    {
      var slice = tokens.Skip(i).Take(maxTokens).ToArray();
      chunks.Add(encoder.Decode(slice));
    }

    return chunks;
  }

  private static string[] RenderPdfToBase64Pages(string path)
  {
    return PDFtoImage.Conversion
      .ToImages(path)
      .Select(bitmap =>
      {
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(data.ToArray());
      })
      .ToArray();
  }
  
  // private static IEnumerable<string> ChunkText(string text, int chunkSize = 500, int overlap = 50)
  // {
  //   var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
  //   for (int i = 0; i < words.Length; i += chunkSize - overlap)
  //   {
  //     yield return string.Join(' ',
  //       words.Skip(i).Take(chunkSize)
  //     );
  //   }
  // }

  // public async Task<string> GetAttachmentSummaryAsync(ChatMessageAttachment att)
  // {
  //   return att.FileType switch
  //   {
  //     "application/pdf" => {
  //       var sb = new StringBuilder();
  //           using var doc = UglyToad.PdfPig.PdfDocument.Open(att.UrlOrPath);
  //       foreach (var page in doc.GetPages())
  //       {
  //         sb.AppendLine(page.Text);
  //       }
  //       return GetTextSummary(sb.ToString());
  //     },
  //     var t when t.StartsWith("image/") => {
  //       var bytes = await File.ReadAllBytesAsync(att.UrlOrPath);
  //       var ext   = Path.GetExtension(att.UrlOrPath).TrimStart('.');
  //       var base64  = Convert.ToBase64String(bytes);
  //       var base64_url = $"data:image/{ext};base64,{base64}";

  //       var reqBody = new
  //       {
  //         model  = "gpt-4.1-mini-2025-04-14",
  //         stream   = false,
  //         messages = new[]
  //         {
  //           new { ChatMessageRole.System, new ChatMessageContent("You are an image‐captioning assistant. Provide a concise, descriptive caption." },)
  //           new { role = "user",   new ChatMessageContent($"Analyze and caption this image:\n\n![]({base64_url})" })
  //         }
  //       };

  //       var resp = await _httpOpenAi.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", reqBody);
  //       resp.EnsureSuccessStatusCode();

  //       using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
  //       return doc.RootElement
  //         .GetProperty("choices")[0]
  //         .GetProperty("message")
  //         .GetProperty("content")
  //         .GetString()!
  //         .Trim();
  //     },
  //     "text/plain" => GetTextSummary(await File.ReadAllTextAsync(att.UrlOrPath));
  //     "text/url" => GetTextSummary(await GetUrlTextAsync(att.UrlOrPath));
  //     _ => null;
  //   };
  // }

  // public async Task<string> GetTextSummary(string text)
  // {
  //   List<OpenAI.Chat.ChatMessage> messages = [
  //     new SystemChatMessage("You are a concise summarizer. Return a clear summary in ≤100 words."),
  //     new UserChatMessage($"Please summarize the following text:\n\n{text}")
  //   ];
  //   ChatCompletion completion = _clientOpenAi.CompleteChat(messages);
  //   return completion.Content[0].Text;
  // }

  // public async Task<string> GetUrlTextAsync(string url)
  // {
  //   var html = await _http.GetStringAsync(url);
  //   var doc = new HtmlAgilityPack.HtmlDocument();
  //   doc.LoadHtml(html);
  //   return string.Join(" ", doc.DocumentNode.SelectNodes("//p").Select(p => p.InnerText.Trim()));
  // }
}