using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetCommentsByPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<CommentDto> AddCommentAsync(Guid postId, int authorId, CreateCommentDto request, CancellationToken cancellationToken = default);
    Task<CommentDto> UpdateCommentAsync(Guid commentId, int actorUserId, bool isAdmin, CreateCommentDto request, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, int actorUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
