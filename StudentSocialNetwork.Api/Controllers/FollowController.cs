using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[ApiController]
[Route("api/follow")]
public class FollowController : ControllerBase
{
    private readonly IFollowService _followService;

    public FollowController(IFollowService followService)
    {
        _followService = followService;
    }

    [Authorize]
    [HttpPost("{userId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ToggleFollow(int userId, CancellationToken cancellationToken)
    {
        var followed = await _followService.ToggleFollowAsync(GetCurrentUserId(), userId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { followed }));
    }

    [HttpGet("{userId:int}/followers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FollowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FollowDto>>>> GetFollowers(int userId, CancellationToken cancellationToken)
    {
        var data = await _followService.GetFollowersAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("{userId:int}/following")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FollowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FollowDto>>>> GetFollowing(int userId, CancellationToken cancellationToken)
    {
        var data = await _followService.GetFollowingAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok(data));
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
