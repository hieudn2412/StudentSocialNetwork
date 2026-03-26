using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IPostService
{
    Task<IReadOnlyList<PostDto>> GetFeedAsync(
        int? currentUserId,
        string tab,
        string? search,
        string? hashtag,
        int? authorId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PostDto> CreatePostAsync(int authorId, CreatePostDto request, CancellationToken cancellationToken = default);
    Task<PostDto> GetPostDetailAsync(Guid postId, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostAsync(Guid postId, int actorUserId, bool isAdmin, UpdatePostDto request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid postId, int actorUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<bool> ToggleLikeAsync(Guid postId, int userId, CancellationToken cancellationToken = default);
}
