using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class AuthService : ApiClientBase, IAuthService
{
    public AuthService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public Task<AuthTokenDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<AuthTokenDto>(HttpMethod.Post, "api/auth/register", request, authorize: false, cancellationToken: cancellationToken);
    }

    public Task<AuthTokenDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<AuthTokenDto>(HttpMethod.Post, "api/auth/login", request, authorize: false, cancellationToken: cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(HttpMethod.Put, "api/auth/change-password", request, authorize: true, cancellationToken: cancellationToken);
    }
}
