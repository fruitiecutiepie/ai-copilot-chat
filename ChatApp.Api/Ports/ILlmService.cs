using ChatApp.Api.Services.Llm;

namespace ChatApp.Api.Ports;

public interface ILlmService
{
  IAsyncEnumerable<string> GetCompletionStreamAsync(
    string convId,
    string senderId,
    string content
  );
  Task<List<OpenAI.Chat.ChatMessage>> GetPromptMessagesAsync(
    string convId,
    string senderId,
    string content
  );
  Task<List<EmbeddingResult>> GetEmbeddingAsync(EmbeddingInputType type, string source);
}
