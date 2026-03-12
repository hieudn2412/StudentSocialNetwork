using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

/// <summary>
/// Handles authentication and identity-related endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, GetClientIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(response, "Login successful."));
    }

    /// <summary>
    /// Authenticates a user with external identity provider credentials.
    /// </summary>
    [HttpPost("external-login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> ExternalLogin([FromBody] ExternalLoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.ExternalLoginAsync(request, GetClientIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(response, "External login successful."));
    }

    /// <summary>
    /// Rotates refresh token and returns a new access/refresh token pair.
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, GetClientIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(response, "Token refreshed successfully."));
    }

    /// <summary>
    /// Revokes refresh token(s) for the current user.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Logout successful."));
    }

    /// <summary>
    /// Returns the current authenticated user profile.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken)
    {
        var me = await _authService.GetMeAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(me));
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

    private string? GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
