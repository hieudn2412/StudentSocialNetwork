using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.Hubs;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;

namespace StudentSocialNetwork.Api.API.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IConversationRepository _conversationRepository;

    public ChatHub(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task JoinConversation(int conversationId)
    {
        await EnsureMembershipAsync(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroups.Conversation(conversationId));
    }

    public Task LeaveConversation(int conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroups.Conversation(conversationId));
    }

    public async Task StartTyping(int conversationId)
    {
        await EnsureMembershipAsync(conversationId);

        await Clients.OthersInGroup(ChatGroups.Conversation(conversationId)).TypingIndicatorUpdated(new TypingIndicatorDto
        {
            ConversationId = conversationId,
            UserId = GetCurrentUserId(),
            Username = GetCurrentUsername(),
            IsTyping = true,
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task StopTyping(int conversationId)
    {
        await EnsureMembershipAsync(conversationId);

        await Clients.OthersInGroup(ChatGroups.Conversation(conversationId)).TypingIndicatorUpdated(new TypingIndicatorDto
        {
            ConversationId = conversationId,
            UserId = GetCurrentUserId(),
            Username = GetCurrentUsername(),
            IsTyping = false,
            OccurredAt = DateTime.UtcNow
        });
    }

    private async Task EnsureMembershipAsync(int conversationId)
    {
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, GetCurrentUserId());
        if (!isMember)
        {
            throw new UnauthorizedException("You are not a member of this conversation.");
        }
    }

    private int GetCurrentUserId()
    {
        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new HubException("Authenticated user id is missing.");
        }

        return userId;
    }

    private string GetCurrentUsername()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Name)
            ?? Context.User?.FindFirstValue("username")
            ?? $"user-{GetCurrentUserId()}";
    }
}
