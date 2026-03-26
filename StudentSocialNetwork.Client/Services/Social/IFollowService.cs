using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface IFollowService
{
    Task<bool> ToggleFollowAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowDto>> GetFollowersAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowDto>> GetFollowingAsync(int userId, CancellationToken cancellationToken = default);
}
