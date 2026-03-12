using Microsoft.AspNetCore.Mvc;

namespace StudentSocialNetwork.Client.Controllers.Api;

[ApiController]
[Route("api/files")]
public class FilesApiController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FilesApiController(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return BadRequest(new { success = false, message = "No files were selected.", data = (object?)null });
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        var uploadsDirectory = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDirectory);

        var uploadedFiles = new List<object>();

        foreach (var file in files)
        {
            if (file.Length <= 0)
            {
                continue;
            }

            var safeFileName = Path.GetFileName(file.FileName);
            var storedFileName = $"{Guid.NewGuid():N}-{safeFileName}";
            var absolutePath = Path.Combine(uploadsDirectory, storedFileName);

            await using (var stream = System.IO.File.Create(absolutePath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{storedFileName}";

            uploadedFiles.Add(new
            {
                FileUrl = fileUrl,
                FileName = safeFileName,
                FileType = file.ContentType,
                FileSize = file.Length
            });
        }

        return Ok(new
        {
            success = true,
            message = "Files uploaded successfully.",
            data = uploadedFiles
        });
    }
}
