using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(int userId, LogoutRequestDto request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetMeAsync(int userId, CancellationToken cancellationToken = default);
}
