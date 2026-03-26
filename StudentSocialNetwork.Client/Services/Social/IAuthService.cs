using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface IAuthService
{
    Task<AuthTokenDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default);
}
