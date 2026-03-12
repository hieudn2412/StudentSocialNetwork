using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Infrastructure.Repositories;

public class ExternalAccountRepository : IExternalAccountRepository
{
    private readonly ChatDbContext _dbContext;

    public ExternalAccountRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ExternalAccount?> GetByProviderAsync(string provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedProviderUserId = providerUserId.Trim().ToLowerInvariant();

        return _dbContext.ExternalAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Provider.ToLower() == normalizedProvider && x.ProviderUserId.ToLower() == normalizedProviderUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalAccount>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExternalAccounts
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ExternalAccount account, CancellationToken cancellationToken = default)
    {
        return _dbContext.ExternalAccounts.AddAsync(account, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
