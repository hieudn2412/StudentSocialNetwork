using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IAdminService
{
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(bool? isActive, CancellationToken cancellationToken = default);
    Task<UserDto> ToggleActiveAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetAllPostsAsync(CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostStatusAsync(Guid postId, PostStatus status, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default);
}
