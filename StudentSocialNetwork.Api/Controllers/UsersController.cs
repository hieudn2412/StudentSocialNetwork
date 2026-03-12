using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.DTOs.Users;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UsersController(IUserProfileService userProfileService, IWebHostEnvironment webHostEnvironment)
    {
        _userProfileService = userProfileService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSearchResultDto>>>> SearchUsers(
        [FromQuery] string q,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var users = await _userProfileService.SearchUsersAsync(GetCurrentUserId(), q, limit, cancellationToken);
        return Ok(ApiResponse.Ok(users));
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> UpdateProfile(
        [FromBody] UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.UpdateProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(profile, "Profile updated."));
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType(typeof(ApiResponse<AvatarUploadResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AvatarUploadResponseDto>>> UploadAvatar(
        [FromForm] IFormFile avatar,
        CancellationToken cancellationToken)
    {
        if (avatar is null || avatar.Length <= 0)
        {
            return BadRequest(ApiResponse.Fail<object>("Avatar file is required."));
        }

        if (!avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse.Fail<object>("Only image files are allowed."));
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        var avatarsDirectory = Path.Combine(webRoot, "uploads", "avatars");
        Directory.CreateDirectory(avatarsDirectory);

        var extension = Path.GetExtension(avatar.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var absolutePath = Path.Combine(avatarsDirectory, storedFileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await avatar.CopyToAsync(stream, cancellationToken);
        }

        var avatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{storedFileName}";
        var profile = await _userProfileService.UpdateAvatarAsync(GetCurrentUserId(), avatarUrl, cancellationToken);

        var response = new AvatarUploadResponseDto
        {
            AvatarUrl = avatarUrl,
            Profile = profile
        };

        return Ok(ApiResponse.Ok(response, "Avatar updated."));
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
}
