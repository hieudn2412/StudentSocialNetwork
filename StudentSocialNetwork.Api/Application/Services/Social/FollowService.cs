using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class FollowService : IFollowService
{
    private readonly ChatDbContext _dbContext;

    public FollowService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ToggleFollowAsync(int followerId, int followingId, CancellationToken cancellationToken = default)
    {
        if (followerId == followingId)
        {
            throw new ArgumentException("Không thể tự follow chính mình.");
        }

        var targetExists = await _dbContext.Users.AnyAsync(x => x.Id == followingId, cancellationToken);
        if (!targetExists)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

        var existingFollow = await _dbContext.Follows
            .FirstOrDefaultAsync(x => x.FollowerId == followerId && x.FollowingId == followingId, cancellationToken);

        if (existingFollow is null)
        {
            _dbContext.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        _dbContext.Follows.Remove(existingFollow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return false;
    }

    public async Task<IReadOnlyList<FollowDto>> GetFollowersAsync(int userId, CancellationToken cancellationToken = default)
    {
        var followers = await _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowingId == userId)
            .Include(x => x.Follower)
                .ThenInclude(x => x.Profile)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return followers.Select(x => x.ToFollowDto(isFollowerSide: true)).ToList();
    }

    public async Task<IReadOnlyList<FollowDto>> GetFollowingAsync(int userId, CancellationToken cancellationToken = default)
    {
        var following = await _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowerId == userId)
            .Include(x => x.Following)
                .ThenInclude(x => x.Profile)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return following.Select(x => x.ToFollowDto(isFollowerSide: false)).ToList();
    }
}
