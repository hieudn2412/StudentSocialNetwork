using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.DTOs.Users;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IUserProfileService
{
    Task<CurrentUserDto> UpdateProfileAsync(int userId, UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> UpdateAvatarAsync(int userId, string avatarUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(int currentUserId, string query, int limit = 10, CancellationToken cancellationToken = default);
}

