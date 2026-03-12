using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ChatDbContext _dbContext;

    public UserRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToList();
        return await _dbContext.Users
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(int currentUserId, string query, int limit, CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || limit <= 0)
        {
            return Array.Empty<User>();
        }

        var lowered = normalized.ToLowerInvariant();
        var safeLimit = Math.Clamp(limit, 1, 30);

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id != currentUserId
                && (x.Username.ToLower().Contains(lowered) || x.Email.ToLower().Contains(lowered)))
            .OrderBy(x => x.Username)
            .ThenBy(x => x.Email)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
