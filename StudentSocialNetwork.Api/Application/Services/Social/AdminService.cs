using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class AdminService : IAdminService
{
    private readonly ChatDbContext _dbContext;

    public AdminService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(x => x.ToUserDto()).ToList();
    }

    public async Task<UserDto> ToggleActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.ToUserDto();
    }

    public async Task<IReadOnlyList<PostDto>> GetAllPostsAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _dbContext.Posts
            .AsNoTracking()
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return posts.Select(x => x.ToPostDto(null)).ToList();
    }

    public async Task<PostDto> UpdatePostStatusAsync(Guid postId, PostStatus status, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        post.Status = status;
        post.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return post.ToPostDto(null);
    }

    public async Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bài viết.");

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
