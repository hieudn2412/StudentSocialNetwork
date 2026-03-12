using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Interfaces.Repositories;

public interface IExternalAccountRepository
{
    Task<ExternalAccount?> GetByProviderAsync(string provider, string providerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalAccount>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalAccount account, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
