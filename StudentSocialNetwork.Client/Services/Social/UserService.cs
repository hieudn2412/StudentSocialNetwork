using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class UserService : ApiClientBase, IUserService
{
    public UserService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public Task<UserDto?> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(HttpMethod.Get, $"api/users/{id}", authorize: true, cancellationToken: cancellationToken);
    }

    public Task<UserDto?> UpdateProfileAsync(int id, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(HttpMethod.Put, $"api/users/{id}", request, authorize: true, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PostDto>> GetPostsByUserAsync(int id, CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<PostDto>>(
            HttpMethod.Get,
            $"api/users/{id}/posts",
            authorize: true,
            cancellationToken: cancellationToken) ?? Array.Empty<PostDto>();
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(query);
        return await SendAsync<IReadOnlyList<UserDto>>(
            HttpMethod.Get,
            $"api/users/search?q={encoded}&limit={limit}",
            authorize: true,
            cancellationToken: cancellationToken) ?? Array.Empty<UserDto>();
    }

    public async Task<IReadOnlyList<UserDto>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<UserDto>>(
            HttpMethod.Get,
            "api/users/suggestions",
            authorize: true,
            cancellationToken: cancellationToken) ?? Array.Empty<UserDto>();
    }
}
