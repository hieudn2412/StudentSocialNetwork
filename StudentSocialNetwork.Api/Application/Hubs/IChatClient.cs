using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);
    Task MessageReactionUpdated(MessageReactionBroadcastDto reaction);
    Task PinnedMessageUpdated(PinnedMessageBroadcastDto pinnedMessage);
    Task TypingIndicatorUpdated(TypingIndicatorDto typingIndicator);
}
