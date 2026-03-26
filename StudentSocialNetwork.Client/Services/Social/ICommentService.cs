using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetByPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<CommentDto?> AddAsync(Guid postId, CreateCommentDto request, CancellationToken cancellationToken = default);
    Task<CommentDto?> UpdateAsync(Guid commentId, CreateCommentDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default);
}
