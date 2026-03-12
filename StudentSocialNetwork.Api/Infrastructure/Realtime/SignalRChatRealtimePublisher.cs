using Microsoft.AspNetCore.SignalR;
using StudentSocialNetwork.Api.API.Hubs;
using StudentSocialNetwork.Api.Application.Hubs;
using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Infrastructure.Realtime;

public class SignalRChatRealtimePublisher : IChatRealtimePublisher
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public SignalRChatRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastMessageCreatedAsync(int conversationId, MessageDto message, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(ChatGroups.Conversation(conversationId))
            .ReceiveMessage(message);
    }

    public Task BroadcastReactionUpdatedAsync(MessageReactionBroadcastDto reaction, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(ChatGroups.Conversation(reaction.ConversationId))
            .MessageReactionUpdated(reaction);
    }

    public Task BroadcastPinnedMessageUpdatedAsync(PinnedMessageBroadcastDto pinnedMessage, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(ChatGroups.Conversation(pinnedMessage.ConversationId))
            .PinnedMessageUpdated(pinnedMessage);
    }
}
