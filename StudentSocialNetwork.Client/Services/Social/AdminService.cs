using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class AdminService : ApiClientBase, IAdminService
{
    public AdminService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public Task<ReportSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<ReportSummaryDto>(HttpMethod.Get, "api/report/summary", authorize: true, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<TopPostDto>> GetTopPostsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<TopPostDto>>(HttpMethod.Get, "api/report/top-posts", authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<TopPostDto>();
    }

    public async Task<IReadOnlyList<ChartDataDto>> GetPostsByDateAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<ChartDataDto>>(HttpMethod.Get, "api/report/posts-by-date", authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<ChartDataDto>();
    }

    public async Task<IReadOnlyList<ChartDataDto>> GetUsersByDateAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<ChartDataDto>>(HttpMethod.Get, "api/report/users-by-date", authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<ChartDataDto>();
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var path = isActive.HasValue
            ? $"api/admin/users?isActive={isActive.Value.ToString().ToLowerInvariant()}"
            : "api/admin/users";

        return await SendAsync<IReadOnlyList<UserDto>>(HttpMethod.Get, path, authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<UserDto>();
    }

    public Task<UserDto?> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(HttpMethod.Put, $"api/admin/users/{id}/toggle-active", new { }, authorize: true, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PostDto>> GetPostsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<PostDto>>(HttpMethod.Get, "api/admin/posts", authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<PostDto>();
    }

    public Task<PostDto?> UpdatePostStatusAsync(Guid id, PostStatus status, CancellationToken cancellationToken = default)
    {
        return SendAsync<PostDto>(HttpMethod.Put, $"api/admin/posts/{id}/status", new { status }, authorize: true, cancellationToken: cancellationToken);
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(HttpMethod.Delete, $"api/admin/posts/{id}", authorize: true, cancellationToken: cancellationToken);
    }
}
