using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class CommentService : ICommentService
{
    private readonly ChatDbContext _dbContext;

    public CommentService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsByPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Posts.AnyAsync(x => x.Id == postId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Không tìm thấy bài viết.");
        }

        var comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.PostId == postId)
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(x => x.ToCommentDto()).ToList();
    }

    public async Task<CommentDto> AddCommentAsync(Guid postId, int authorId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        if (post.Status != PostStatus.Approved && post.AuthorId != authorId)
        {
            throw new ForbiddenException("Bạn không thể bình luận bài viết này.");
        }

        var comment = new Comment
        {
            PostId = postId,
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedComment = await _dbContext.Comments
            .AsNoTracking()
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .FirstAsync(x => x.Id == comment.Id, cancellationToken);

        return savedComment.ToCommentDto();
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, int actorUserId, bool isAdmin, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        _ = isAdmin;

        var comment = await _dbContext.Comments
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bình luận.");

        if (comment.AuthorId != actorUserId)
        {
            throw new ForbiddenException("Bạn không có quyền sửa bình luận này.");
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return comment.ToCommentDto();
    }

    public async Task DeleteCommentAsync(Guid commentId, int actorUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bình luận.");

        if (!isAdmin && comment.AuthorId != actorUserId)
        {
            throw new ForbiddenException("Bạn không có quyền xoá bình luận này.");
        }

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
