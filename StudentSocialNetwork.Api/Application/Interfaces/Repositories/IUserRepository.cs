using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> SearchAsync(int currentUserId, string query, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

