namespace ChatApp.Api;

public class AppSettings
{
  public OpenAISettings OpenAI { get; set; } = default!;
  public CohereSettings Cohere { get; set; } = default!;
}

public class OpenAISettings
{
  public string ApiKey { get; set; } = default!;
  public string Model { get; set; } = default!;
}

public class CohereSettings
{
  public string ApiKey { get; set; } = default!;
}
