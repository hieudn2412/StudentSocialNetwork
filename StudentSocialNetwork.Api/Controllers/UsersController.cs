using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile(int id, CancellationToken cancellationToken)
    {
        var profile = await _userService.GetProfileAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok(profile));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile(
        int id,
        [FromBody] UpdateProfileDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != id)
        {
            throw new ForbiddenException("Bạn chỉ có thể cập nhật hồ sơ của chính mình.");
        }

        var profile = await _userService.UpdateProfileAsync(currentUserId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok(profile, "Cập nhật hồ sơ thành công."));
    }

    [HttpGet("{id:int}/posts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PostDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PostDto>>>> GetPostsByUser(int id, CancellationToken cancellationToken)
    {
        var currentUserId = TryGetCurrentUserId();
        var posts = await _userService.GetPostsByUserAsync(id, currentUserId, cancellationToken);
        return Ok(ApiResponse.Ok(posts));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> SearchUsers(
        [FromQuery(Name = "q")] string query,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = TryGetCurrentUserId();
        var users = await _userService.SearchUsersAsync(query, currentUserId, limit, cancellationToken);
        return Ok(ApiResponse.Ok(users));
    }

    [Authorize]
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> Suggestions(CancellationToken cancellationToken)
    {
        var users = await _userService.GetSuggestionsAsync(GetCurrentUserId(), 5, cancellationToken);
        return Ok(ApiResponse.Ok(users));
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
}
