using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface IUserService
{
    Task<UserDto?> GetProfileAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateProfileAsync(int id, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetPostsByUserAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetSuggestionsAsync(CancellationToken cancellationToken = default);
}
