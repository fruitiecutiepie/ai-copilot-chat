namespace ChatApp.Api.Services.FileStorage;

public class FileStorage : IFileStorage
{
  private readonly string _basePath;
  public FileStorage(IWebHostEnvironment env)
  {
    _basePath = Path.Combine(env.ContentRootPath, "Data/uploads");
    Directory.CreateDirectory(_basePath);
  }

  public async Task<string> SetChatMessageAttachment(
    string conversationId, Stream fileStream, string filename)
  {
    var convoDir = Path.Combine(_basePath, conversationId);
    Directory.CreateDirectory(convoDir);

    var filePath = Path.Combine(convoDir, filename);
    using var outFs = File.Create(filePath);
    await fileStream.CopyToAsync(outFs);

    // return relative URL path, e.g. "uploads/{conv}/{filename}"
    return Path.Combine("Data/uploads", conversationId, filename)
           .Replace("\\", "/");
  }

  public Task<Stream> GetChatMessageAttachment(string relativePath)
  {
    var full = Path.Combine(_basePath,
      relativePath.Replace("Data/uploads/", ""));
    Stream fs = File.OpenRead(full);
    return Task.FromResult(fs);
  }

  public Task DelChatMessageAttachment(string relativePath)
  {
    var full = Path.Combine(_basePath,
      relativePath.Replace("Data/uploads/", ""));
    File.Delete(full);
    return Task.CompletedTask;
  }
}
