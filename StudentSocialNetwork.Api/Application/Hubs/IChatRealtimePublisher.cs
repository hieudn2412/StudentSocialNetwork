using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Hubs;

public interface IChatRealtimePublisher
{
    Task BroadcastMessageCreatedAsync(int conversationId, MessageDto message, CancellationToken cancellationToken = default);
    Task BroadcastReactionUpdatedAsync(MessageReactionBroadcastDto reaction, CancellationToken cancellationToken = default);
    Task BroadcastPinnedMessageUpdatedAsync(PinnedMessageBroadcastDto pinnedMessage, CancellationToken cancellationToken = default);
}
