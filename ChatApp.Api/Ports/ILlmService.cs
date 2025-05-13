using ChatApp.Api.Services.Llm;

namespace ChatApp.Api.Ports;

public interface ILlmService
{
  IAsyncEnumerable<string> StreamCompletionAsync(
    string convId,
    string userId,
    string content
  );
  Task<List<OpenAI.Chat.ChatMessage>> GetPromptMessagesAsync(
    string convId,
    string userId,
    string content
  );
  Task<List<EmbeddingResult>> GetEmbeddingAsync(EmbeddingInputType type, string source);
}
