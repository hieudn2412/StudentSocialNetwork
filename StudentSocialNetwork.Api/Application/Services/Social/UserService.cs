using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class UserService : IUserService
{
    private readonly ChatDbContext _dbContext;
    private readonly IUserRepository _userRepository;

    public UserService(ChatDbContext dbContext, IUserRepository userRepository)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
    }

    public async Task<UserDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        return user.ToUserDto();
    }

    public async Task<UserDto> UpdateProfileAsync(int actorUserId, int targetUserId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        if (actorUserId != targetUserId)
        {
            throw new ForbiddenException("Bạn chỉ có thể cập nhật hồ sơ của chính mình.");
        }

        var user = await _dbContext.Users
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.Id == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var username = request.Username.Trim();
        if (await _userRepository.ExistsByUsernameAsync(username, user.Id, cancellationToken))
        {
            throw new InvalidOperationException("Username đã tồn tại.");
        }

        user.Username = username;
        user.UpdatedAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        user.Profile ??= new UserProfile
        {
            UserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.Profile.FullName = NormalizeNullable(request.FullName, 200);
        user.Profile.AvatarUrl = NormalizeNullable(request.AvatarUrl, 1000);
        user.Profile.Bio = NormalizeNullable(request.Bio, 1000);
        user.Profile.ClassName = NormalizeNullable(request.ClassName, 100);
        user.Profile.Major = NormalizeNullable(request.Major, 150);
        user.Profile.Interests = NormalizeNullable(request.Interests, 2000);
        user.Profile.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.ToUserDto();
    }

    public async Task<IReadOnlyList<PostDto>> GetPostsByUserAsync(int userId, int? currentUserId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.AuthorId == userId)
            .Include(x => x.Author)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        if (currentUserId is int me && me == userId)
        {
            return await query
                .Select(x => x.ToPostDto(currentUserId))
                .ToListAsync(cancellationToken);
        }

        return await query
            .Where(x => x.Status != PostStatus.Rejected)
            .Select(x => x.ToPostDto(currentUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, int? currentUserId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<UserDto>();
        }

        var safeLimit = Math.Clamp(limit, 1, 50);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .Where(x => !currentUserId.HasValue || x.Id != currentUserId.Value)
            .Where(x =>
                x.Username.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) ||
                (x.Profile != null && (
                    (x.Profile.FullName ?? string.Empty).ToLower().Contains(term) ||
                    (x.Profile.Major ?? string.Empty).ToLower().Contains(term))))
            .OrderBy(x => x.Username)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return users.Select(x => x.ToUserDto()).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> GetSuggestionsAsync(int currentUserId, int take = 5, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 20);

        var followedUserIds = await _dbContext.Follows
            .Where(x => x.FollowerId == currentUserId)
            .Select(x => x.FollowingId)
            .ToListAsync(cancellationToken);

        var suggestions = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Profile)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .Where(x => x.Id != currentUserId)
            .Where(x => !followedUserIds.Contains(x.Id))
            .OrderByDescending(x => x.Followers.Count)
            .ThenByDescending(x => x.CreatedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        return suggestions.Select(x => x.ToUserDto()).ToList();
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
