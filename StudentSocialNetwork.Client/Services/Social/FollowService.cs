using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class FollowService : ApiClientBase, IFollowService
{
    public FollowService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public async Task<bool> ToggleFollowAsync(int userId, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<ToggleFollowResult>(HttpMethod.Post, $"api/follow/{userId}", authorize: true, cancellationToken: cancellationToken);
        return result?.Followed ?? false;
    }

    public async Task<IReadOnlyList<FollowDto>> GetFollowersAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<FollowDto>>(
            HttpMethod.Get,
            $"api/follow/{userId}/followers",
            authorize: false,
            cancellationToken: cancellationToken) ?? Array.Empty<FollowDto>();
    }

    public async Task<IReadOnlyList<FollowDto>> GetFollowingAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<FollowDto>>(
            HttpMethod.Get,
            $"api/follow/{userId}/following",
            authorize: false,
            cancellationToken: cancellationToken) ?? Array.Empty<FollowDto>();
    }

    private sealed class ToggleFollowResult
    {
        public bool Followed { get; set; }
    }
}
