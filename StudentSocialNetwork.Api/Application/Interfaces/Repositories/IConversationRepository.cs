using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByIdWithMembersAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task<ConversationMember?> GetActiveMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetPrivateConversationByMembersAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
