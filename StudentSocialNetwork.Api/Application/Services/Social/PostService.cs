using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class PostService : IPostService
{
    private readonly ChatDbContext _dbContext;

    public PostService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PostDto>> GetFeedAsync(
        int? currentUserId,
        string tab,
        string? search,
        string? hashtag,
        int? authorId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Posts
            .AsNoTracking()
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .AsVisiblePosts(currentUserId, isAdmin);

        if (authorId.HasValue && authorId.Value > 0)
        {
            query = query.Where(x => x.AuthorId == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Content.ToLower().Contains(normalizedSearch) ||
                x.Author.Username.ToLower().Contains(normalizedSearch) ||
                (x.Author.Profile != null && (x.Author.Profile.FullName ?? string.Empty).ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(hashtag))
        {
            var normalizedHashtag = hashtag.Trim().ToLowerInvariant();
            if (!normalizedHashtag.StartsWith('#'))
            {
                normalizedHashtag = $"#{normalizedHashtag}";
            }

            query = query.Where(x => x.Hashtags.ToLower().Contains(normalizedHashtag));
        }

        if (string.Equals(tab, "following", StringComparison.OrdinalIgnoreCase) && currentUserId.HasValue)
        {
            var me = currentUserId.Value;
            var followingIds = _dbContext.Follows
                .Where(x => x.FollowerId == me)
                .Select(x => x.FollowingId);

            query = query.Where(x => x.AuthorId == me || followingIds.Contains(x.AuthorId));
        }

        var posts = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return posts.Select(x => x.ToPostDto(currentUserId)).ToList();
    }

    public async Task<PostDto> CreatePostAsync(int authorId, CreatePostDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == authorId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khoá");
        }

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Content = request.Content.Trim(),
            ImageUrl = NormalizeNullable(request.ImageUrl, 1000),
            Hashtags = SocialMapping.ExtractHashtags(request.Content),
            Status = PostStatus.Approved,
            AuthorId = authorId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedPost = await _dbContext.Posts
            .AsNoTracking()
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .FirstAsync(x => x.Id == post.Id, cancellationToken);

        return savedPost.ToPostDto(authorId);
    }

    public async Task<PostDto> GetPostDetailAsync(Guid postId, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .AsNoTracking()
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        var canView = isAdmin
                      || post.Status != PostStatus.Rejected
                      || (currentUserId.HasValue && post.AuthorId == currentUserId.Value);

        if (!canView)
        {
            throw new KeyNotFoundException("Không tìm thấy bài viết.");
        }

        return post.ToPostDto(currentUserId);
    }

    public async Task<PostDto> UpdatePostAsync(Guid postId, int actorUserId, bool isAdmin, UpdatePostDto request, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        if (!isAdmin && post.AuthorId != actorUserId)
        {
            throw new ForbiddenException("Bạn không có quyền sửa bài viết này.");
        }

        post.Content = request.Content.Trim();
        post.ImageUrl = NormalizeNullable(request.ImageUrl, 1000);
        post.Hashtags = SocialMapping.ExtractHashtags(request.Content);
        post.UpdatedAt = DateTime.UtcNow;
        post.Status = PostStatus.Approved;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return post.ToPostDto(actorUserId);
    }

    public async Task DeletePostAsync(Guid postId, int actorUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        if (!isAdmin && post.AuthorId != actorUserId)
        {
            throw new ForbiddenException("Bạn không có quyền xoá bài viết này.");
        }

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ToggleLikeAsync(Guid postId, int userId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        if (post.Status == PostStatus.Rejected && post.AuthorId != userId)
        {
            throw new ForbiddenException("Bạn không thể thích bài viết này.");
        }

        var existingLike = await _dbContext.Likes.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
        if (existingLike is null)
        {
            _dbContext.Likes.Add(new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        _dbContext.Likes.Remove(existingLike);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return false;
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
