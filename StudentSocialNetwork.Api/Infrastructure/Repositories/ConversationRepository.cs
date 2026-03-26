using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _dbContext;

    public ConversationRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Conversation?> GetByIdAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations
            .Include(x => x.LastMessage)
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
    }

    public Task<Conversation?> GetByIdWithMembersAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations
            .Include(x => x.LastMessage)
            .Include(x => x.Members)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(x => x.Members.Any(m => m.UserId == userId && m.LeftAt == null))
            .Include(x => x.LastMessage)
            .Include(x => x.Members.Where(m => m.LeftAt == null))
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsUserMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ConversationMembers.AnyAsync(
            x => x.ConversationId == conversationId && x.UserId == userId && x.LeftAt == null,
            cancellationToken);
    }

    public Task<ConversationMember?> GetActiveMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ConversationMembers
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId && x.LeftAt == null, cancellationToken);
    }

    public Task<Conversation?> GetPrivateConversationByMembersAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations
            .AsNoTracking()
            .Where(x => x.Type == "Private")
            .Where(x => x.Members.Count(m => m.LeftAt == null) == 2)
            .Where(x => x.Members.Any(m => m.UserId == userId && m.LeftAt == null))
            .Where(x => x.Members.Any(m => m.UserId == otherUserId && m.LeftAt == null))
            .Include(x => x.LastMessage)
            .Include(x => x.Members.Where(m => m.LeftAt == null))
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations.AddAsync(conversation, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
