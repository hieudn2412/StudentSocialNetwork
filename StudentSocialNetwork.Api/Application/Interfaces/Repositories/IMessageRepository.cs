using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(long messageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Message>> GetPageByConversationAsync(
        int conversationId,
        int take,
        DateTime? cursorCreatedAt,
        long? cursorMessageId,
        CancellationToken cancellationToken = default);
    Task<long?> GetLatestMessageIdAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetUnreadMessageIdsAsync(
        int conversationId,
        int userId,
        long? lastReadMessageId,
        long upToMessageId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Message message, CancellationToken cancellationToken = default);

    Task<MessageReaction?> GetReactionAsync(long messageId, int userId, string reactionType, CancellationToken cancellationToken = default);
    Task AddReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default);
    void RemoveReaction(MessageReaction reaction);

    Task<MessageRead?> GetReadReceiptAsync(long messageId, int userId, CancellationToken cancellationToken = default);
    Task AddReadReceiptAsync(MessageRead readReceipt, CancellationToken cancellationToken = default);

    Task<PinnedMessage?> GetPinnedMessageAsync(int conversationId, long messageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PinnedMessage>> GetPinnedMessagesAsync(int conversationId, CancellationToken cancellationToken = default);
    Task AddPinnedMessageAsync(PinnedMessage pinnedMessage, CancellationToken cancellationToken = default);
    void RemovePinnedMessage(PinnedMessage pinnedMessage);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
