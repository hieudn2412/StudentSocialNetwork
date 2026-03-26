using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(int actorUserId, int targetUserId, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetPostsByUserAsync(int userId, int? currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, int? currentUserId, int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetSuggestionsAsync(int currentUserId, int take = 5, CancellationToken cancellationToken = default);
}
