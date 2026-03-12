using StudentSocialNetwork.Api.Application.DTOs.Conversations;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationSummaryDto>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> CreatePrivateConversationAsync(int userId, CreatePrivateConversationRequestDto request, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> CreateGroupConversationAsync(int userId, CreateGroupConversationRequestDto request, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> AddMemberAsync(int actorUserId, int conversationId, AddConversationMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> RemoveMemberAsync(int actorUserId, int conversationId, int memberUserId, CancellationToken cancellationToken = default);
    Task LeaveConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
}
