using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Services.Chat.Ui;

[ApiController]
[Route("api/chat")]
public partial class ChatController : ControllerBase
{
  private readonly IChatService _chatService;
  private readonly IWebHostEnvironment _env;

  public ChatController(
    IChatService chatService,
    IWebHostEnvironment env
  )
  {
    _chatService = chatService;
    _env = env;
  }

  [HttpPost("upload")]
  public async Task<IActionResult> SetChatUploads(
    [FromForm] List<IFormFile> files
  )
  {
    if (files == null || files.Count == 0)
      return BadRequest("No files uploaded.");

    var uploadsPath = Path.Combine(_env.ContentRootPath, "UserData/uploads");

    var urls = new List<string>();
    foreach (var file in files)
    {
      if (file.Length > 0)
      {
        // Create a unique file name (you could also use Nanoid)
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await file.CopyToAsync(stream);
        }
        // Build the URL to the file (ensure Program.cs is set up to serve static files from /uploads)
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        urls.Add($"{baseUrl}/UserData/uploads/{fileName}");
      }
    }
    return Ok(new { urls });
  }
}
