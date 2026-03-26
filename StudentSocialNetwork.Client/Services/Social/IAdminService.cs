using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface IAdminService
{
    Task<ReportSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopPostDto>> GetTopPostsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartDataDto>> GetPostsByDateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartDataDto>> GetUsersByDateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(bool? isActive, CancellationToken cancellationToken = default);
    Task<UserDto?> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostDto>> GetPostsAsync(CancellationToken cancellationToken = default);
    Task<PostDto?> UpdatePostStatusAsync(Guid id, PostStatus status, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default);
}
