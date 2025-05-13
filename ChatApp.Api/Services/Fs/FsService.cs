using ChatApp.Api.Ports;

namespace ChatApp.Api.Services.Fs;

public class FsService : IFsService
{
  private readonly string _basePath;
  private readonly IHttpContextAccessor _httpCtx;
  private static IWebHostEnvironment _env = default!;

  public FsService(
    IWebHostEnvironment env,
    IHttpContextAccessor httpContextAccessor
  ) {
    _basePath = Path.Combine(env.ContentRootPath, "UserData/uploads");
    Directory.CreateDirectory(_basePath);

    _httpCtx = httpContextAccessor;
    _env = env;
  }

  public string FileNameToPublicUrl(string convId, string fileName)
  {
    var req = _httpCtx.HttpContext!.Request;
    return $"{req.Scheme}://{req.Host}/uploads/{convId}/{fileName}";
  }

  public static string UrlToLocalPath(string convId, string publicUrl)
  {
    // throws if not a URI
    var uri = new Uri(publicUrl);

    // ensure it’s our uploads endpoint
    var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length != 2 || segments[0] != "uploads")
      throw new InvalidOperationException("Not an uploads URL");

    var fileName = segments[1];
    var uploadsFolder = Path.Combine(_env.ContentRootPath, "UserData", "uploads", convId);
    return Path.Combine(uploadsFolder, fileName);
  }

  public static string FileNameToLocalPath(string convId, string fileName)
  {
    return Path.Combine("UserData/uploads", convId, fileName).Replace("\\", "/");
  }

  public async Task<string> SetChatMessageAttachmentAsync(
    string convId,
    Stream fileStream,
    string fileName
  )
  {
    var convoDir = Path.Combine(_basePath, convId);
    Directory.CreateDirectory(convoDir);

    var filePath = Path.Combine(convoDir, fileName);
    using var outFs = File.Create(filePath);
    await fileStream.CopyToAsync(outFs);

    // return relative URL path, e.g. "uploads/{convId}/{fileName}"
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
