using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PostsController(IPostService postService, IWebHostEnvironment webHostEnvironment)
    {
        _postService = postService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PostDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PostDto>>>> GetFeed(
        [FromQuery] string tab = "all",
        [FromQuery] string? search = null,
        [FromQuery] string? hashtag = null,
        [FromQuery] int? author = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = TryGetCurrentUserId();
        var isAdmin = IsAdmin();

        var posts = await _postService.GetFeedAsync(
            currentUserId,
            tab,
            search,
            hashtag,
            author,
            isAdmin,
            cancellationToken);

        return Ok(ApiResponse.Ok(posts));
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PostDto>>> CreatePost([FromForm] CreatePostFormRequest request, CancellationToken cancellationToken)
    {
        var imageUrl = await SaveImageIfProvidedAsync(request.Image, cancellationToken);
        var post = await _postService.CreatePostAsync(
            GetCurrentUserId(),
            new CreatePostDto
            {
                Content = request.Content,
                ImageUrl = imageUrl
            },
            cancellationToken);

        return Ok(ApiResponse.Ok(post, "Đã tạo bài viết. Bài viết đang chờ duyệt."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PostDto>>> GetPostDetail(Guid id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetPostDetailAsync(id, TryGetCurrentUserId(), IsAdmin(), cancellationToken);
        return Ok(ApiResponse.Ok(post));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PostDto>>> UpdatePost(
        Guid id,
        [FromBody] UpdatePostDto request,
        CancellationToken cancellationToken)
    {
        var post = await _postService.UpdatePostAsync(id, GetCurrentUserId(), IsAdmin(), request, cancellationToken);
        return Ok(ApiResponse.Ok(post, "Cập nhật bài viết thành công."));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        await _postService.DeletePostAsync(id, GetCurrentUserId(), IsAdmin(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Đã xoá bài viết."));
    }

    [Authorize]
    [HttpPost("{id:guid}/like")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ToggleLike(Guid id, CancellationToken cancellationToken)
    {
        var liked = await _postService.ToggleLikeAsync(id, GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { liked }));
    }

    private async Task<string?> SaveImageIfProvidedAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Chỉ hỗ trợ file ảnh.");
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        var folder = Path.Combine(webRoot, "uploads", "posts");
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(file.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var absolutePath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"{Request.Scheme}://{Request.Host}/uploads/posts/{fileName}";
    }

    private int GetCurrentUserId()
    {
        var subjectClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(subjectClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }

    private int? TryGetCurrentUserId()
    {
        var subjectClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(subjectClaim, out var userId) ? userId : null;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    public sealed class CreatePostFormRequest
    {
        public string Content { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
