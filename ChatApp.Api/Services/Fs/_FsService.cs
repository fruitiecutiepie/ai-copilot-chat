namespace ChatApp.Api.Services.FsService;

public class FsService : IFsService
{
  private readonly string _basePath;
  public FsService(IWebHostEnvironment env)
  {
    _basePath = Path.Combine(env.ContentRootPath, "UserData/uploads");
    Directory.CreateDirectory(_basePath);
  }

  public async Task<string> SetChatMessageAttachmentAsync(
    string convId,
    Stream fileStream,
    string fileName
  ) {
    var convoDir = Path.Combine(_basePath, convId);
    Directory.CreateDirectory(convoDir);

    var filePath = Path.Combine(convoDir, fileName);
    using var outFs = File.Create(filePath);
    await fileStream.CopyToAsync(outFs);

    // return relative URL path, e.g. "uploads/{conv}/{fileName}"
    return Path.Combine("UserData/uploads", convId, fileName)
           .Replace("\\", "/");
  }

  public Task<Stream> GetChatMessageAttachment(string relativePath)
  {
    var full = Path.Combine(_basePath,
      relativePath.Replace("UserData/uploads/", ""));
    Stream fs = File.OpenRead(full);
    return Task.FromResult(fs);
  }

  public Task DelChatMessageAttachment(string relativePath)
  {
    var full = Path.Combine(_basePath,
      relativePath.Replace("UserData/uploads/", ""));
    File.Delete(full);
    return Task.CompletedTask;
  }
}
