using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _dbContext;

    public MessageRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Message?> GetByIdAsync(long messageId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Messages
            .Include(x => x.Sender)
            .Include(x => x.Attachments)
            .Include(x => x.Reactions)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetPageByConversationAsync(
        int conversationId,
        int take,
        DateTime? cursorCreatedAt,
        long? cursorMessageId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId);

        if (cursorCreatedAt.HasValue && cursorMessageId.HasValue)
        {
            var dateCursor = cursorCreatedAt.Value;
            var idCursor = cursorMessageId.Value;
            query = query.Where(x => x.CreatedAt < dateCursor || (x.CreatedAt == dateCursor && x.Id < idCursor));
        }

        return await query
            .Include(x => x.Sender)
            .Include(x => x.Attachments)
            .Include(x => x.Reactions)
                .ThenInclude(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<long?> GetLatestMessageIdAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Messages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<long>> GetUnreadMessageIdsAsync(
        int conversationId,
        int userId,
        long? lastReadMessageId,
        long upToMessageId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .Where(x => x.Id <= upToMessageId);

        if (lastReadMessageId.HasValue)
        {
            query = query.Where(x => x.Id > lastReadMessageId.Value);
        }

        return await query
            .Where(x => !_dbContext.MessageReads.Any(r => r.MessageId == x.Id && r.UserId == userId))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        return _dbContext.Messages.AddAsync(message, cancellationToken).AsTask();
    }

    public Task<MessageReaction?> GetReactionAsync(long messageId, int userId, string reactionType, CancellationToken cancellationToken = default)
    {
        var normalizedReaction = reactionType.Trim().ToLowerInvariant();
        return _dbContext.MessageReactions
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.MessageId == messageId && x.UserId == userId && x.ReactionType.ToLower() == normalizedReaction,
                cancellationToken);
    }

    public Task AddReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        return _dbContext.MessageReactions.AddAsync(reaction, cancellationToken).AsTask();
    }

    public void RemoveReaction(MessageReaction reaction)
    {
        _dbContext.MessageReactions.Remove(reaction);
    }

    public Task<MessageRead?> GetReadReceiptAsync(long messageId, int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MessageReads
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.UserId == userId, cancellationToken);
    }

    public Task AddReadReceiptAsync(MessageRead readReceipt, CancellationToken cancellationToken = default)
    {
        return _dbContext.MessageReads.AddAsync(readReceipt, cancellationToken).AsTask();
    }

    public Task<PinnedMessage?> GetPinnedMessageAsync(int conversationId, long messageId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PinnedMessages
            .Include(x => x.PinnedByUser)
            .Include(x => x.Message)
                .ThenInclude(x => x.Sender)
            .Include(x => x.Message)
                .ThenInclude(x => x.Attachments)
            .Include(x => x.Message)
                .ThenInclude(x => x.Reactions)
                    .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.MessageId == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<PinnedMessage>> GetPinnedMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PinnedMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .Include(x => x.PinnedByUser)
            .Include(x => x.Message)
                .ThenInclude(x => x.Sender)
            .Include(x => x.Message)
                .ThenInclude(x => x.Attachments)
            .Include(x => x.Message)
                .ThenInclude(x => x.Reactions)
                    .ThenInclude(x => x.User)
            .OrderByDescending(x => x.PinnedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddPinnedMessageAsync(PinnedMessage pinnedMessage, CancellationToken cancellationToken = default)
    {
        return _dbContext.PinnedMessages.AddAsync(pinnedMessage, cancellationToken).AsTask();
    }

    public void RemovePinnedMessage(PinnedMessage pinnedMessage)
    {
        _dbContext.PinnedMessages.Remove(pinnedMessage);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
