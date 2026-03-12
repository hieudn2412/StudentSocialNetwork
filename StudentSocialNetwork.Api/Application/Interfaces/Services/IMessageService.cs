using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IMessageService
{
    Task<CursorPagedResultDto<MessageDto>> GetMessagesAsync(int userId, int conversationId, string? cursor, int limit, CancellationToken cancellationToken = default);
    Task<MessageDto> SendMessageAsync(int userId, int conversationId, SendMessageRequestDto request, CancellationToken cancellationToken = default);
    Task<MessageReactionDto> AddReactionAsync(int userId, long messageId, AddMessageReactionRequestDto request, CancellationToken cancellationToken = default);
    Task RemoveReactionAsync(int userId, long messageId, string reactionType, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int userId, long messageId, CancellationToken cancellationToken = default);
    Task MarkConversationAsReadAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PinnedMessageDto>> GetPinnedMessagesAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
    Task PinMessageAsync(int userId, int conversationId, long messageId, CancellationToken cancellationToken = default);
    Task UnpinMessageAsync(int userId, int conversationId, long messageId, CancellationToken cancellationToken = default);
}
