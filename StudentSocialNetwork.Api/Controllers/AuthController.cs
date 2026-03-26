using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Register([FromBody] RegisterDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok(response, "Đăng ký thành công."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Login([FromBody] LoginDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok(response, "Đăng nhập thành công."));
    }

    [Authorize]
    [HttpPut("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Đổi mật khẩu thành công."));
    }

    // Legacy chat-auth endpoints kept for backward compatibility.
    [HttpPost("external-login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> ExternalLogin([FromBody] ExternalLoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.ExternalLoginAsync(request, GetClientIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(response, "External login successful."));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, GetClientIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(response, "Token refreshed successfully."));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Logout successful."));
    }

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
