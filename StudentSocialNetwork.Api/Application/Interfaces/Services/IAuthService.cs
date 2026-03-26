using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthTokenDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, ChangePasswordDto request, CancellationToken cancellationToken = default);

    // Legacy chat-auth endpoints kept for backward compatibility.
    Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(int userId, LogoutRequestDto request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetMeAsync(int userId, CancellationToken cancellationToken = default);
}
