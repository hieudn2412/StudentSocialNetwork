using StudentSocialNetwork.Api.Application.DTOs.Conversations;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;

    public ConversationService(IConversationRepository conversationRepository, IUserRepository userRepository)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var conversations = await _conversationRepository.GetForUserAsync(userId, cancellationToken);
        return conversations.Select(MapConversation).ToList();
    }

    public async Task<ConversationSummaryDto> CreatePrivateConversationAsync(int userId, CreatePrivateConversationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.OtherUserId == userId)
        {
            throw new ArgumentException("Cannot create a private conversation with yourself.");
        }

        var otherUser = await _userRepository.GetByIdAsync(request.OtherUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Target user does not exist.");

        var existingConversation = await _conversationRepository.GetPrivateConversationByMembersAsync(userId, otherUser.Id, cancellationToken);
        if (existingConversation is not null)
        {
            return MapConversation(existingConversation);
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Type = "Private",
            CreatedBy = userId,
            CreatedAt = now
        };

        conversation.Members.Add(new ConversationMember
        {
            UserId = userId,
            Role = "Member",
            JoinedAt = now
        });

        conversation.Members.Add(new ConversationMember
        {
            UserId = otherUser.Id,
            Role = "Member",
            JoinedAt = now
        });

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var createdConversation = await _conversationRepository.GetByIdWithMembersAsync(conversation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Conversation was created but could not be loaded.");

        return MapConversation(createdConversation);
    }

    public async Task<ConversationSummaryDto> CreateGroupConversationAsync(int userId, CreateGroupConversationRequestDto request, CancellationToken cancellationToken = default)
    {
        var memberIds = request.MemberIds.Distinct().Where(x => x != userId).ToHashSet();
        if (memberIds.Count == 0)
        {
            throw new ArgumentException("Group conversation requires at least one additional member.");
        }

        var users = await _userRepository.GetByIdsAsync(memberIds, cancellationToken);
        if (users.Count != memberIds.Count)
        {
            throw new KeyNotFoundException("One or more members do not exist.");
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Type = "Group",
            Name = request.Name.Trim(),
            AvatarUrl = request.AvatarUrl?.Trim(),
            CreatedBy = userId,
            CreatedAt = now
        };

        conversation.Members.Add(new ConversationMember
        {
            UserId = userId,
            Role = "Admin",
            JoinedAt = now
        });

        foreach (var memberId in memberIds)
        {
            conversation.Members.Add(new ConversationMember
            {
                UserId = memberId,
                Role = "Member",
                JoinedAt = now
            });
        }

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var createdConversation = await _conversationRepository.GetByIdWithMembersAsync(conversation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Conversation was created but could not be loaded.");

        return MapConversation(createdConversation);
    }

    public async Task<ConversationSummaryDto> AddMemberAsync(int actorUserId, int conversationId, AddConversationMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdWithMembersAsync(conversationId, cancellationToken)
            ?? throw new KeyNotFoundException("Conversation not found.");

        EnsureGroupConversation(conversation);

        var actorMembership = GetActiveMembership(conversation, actorUserId);
        if (actorMembership is null || !IsAdmin(actorMembership))
        {
            throw new UnauthorizedAccessException("Only admins can add members.");
        }

        var targetUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User does not exist.");

        var existingMembership = conversation.Members.FirstOrDefault(x => x.UserId == targetUser.Id);
        if (existingMembership is not null && existingMembership.LeftAt is null)
        {
            throw new InvalidOperationException("User is already a member of this conversation.");
        }

        if (existingMembership is null)
        {
            conversation.Members.Add(new ConversationMember
            {
                UserId = targetUser.Id,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Member" : request.Role.Trim(),
                JoinedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingMembership.LeftAt = null;
            existingMembership.JoinedAt = DateTime.UtcNow;
            existingMembership.Role = string.IsNullOrWhiteSpace(request.Role) ? "Member" : request.Role.Trim();
        }

        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var updatedConversation = await _conversationRepository.GetByIdWithMembersAsync(conversation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Conversation update could not be loaded.");

        return MapConversation(updatedConversation);
    }

    public async Task<ConversationSummaryDto> RemoveMemberAsync(int actorUserId, int conversationId, int memberUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdWithMembersAsync(conversationId, cancellationToken)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var actorMembership = GetActiveMembership(conversation, actorUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this conversation.");

        var targetMembership = GetActiveMembership(conversation, memberUserId)
            ?? throw new KeyNotFoundException("Target member is not in this conversation.");

        var isSelfAction = actorUserId == memberUserId;
        if (!isSelfAction && !IsAdmin(actorMembership))
        {
            throw new UnauthorizedAccessException("Only admins can remove other members.");
        }

        targetMembership.LeftAt = DateTime.UtcNow;

        if (IsAdmin(targetMembership))
        {
            EnsureAtLeastOneAdmin(conversation, memberUserId);
        }

        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var updatedConversation = await _conversationRepository.GetByIdWithMembersAsync(conversation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Conversation update could not be loaded.");

        return MapConversation(updatedConversation);
    }

    public async Task LeaveConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        await RemoveMemberAsync(userId, conversationId, userId, cancellationToken);
    }

    private static ConversationMember? GetActiveMembership(Conversation conversation, int userId)
    {
        return conversation.Members.FirstOrDefault(x => x.UserId == userId && x.LeftAt is null);
    }

    private static bool IsAdmin(ConversationMember member)
    {
        return string.Equals(member.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureAtLeastOneAdmin(Conversation conversation, int removedUserId)
    {
        var activeMembers = conversation.Members.Where(x => x.LeftAt is null && x.UserId != removedUserId).ToList();
        if (activeMembers.Count == 0)
        {
            return;
        }

        if (activeMembers.Any(IsAdmin))
        {
            return;
        }

        var fallbackAdmin = activeMembers.OrderBy(x => x.JoinedAt).First();
        fallbackAdmin.Role = "Admin";
    }

    private static void EnsureGroupConversation(Conversation conversation)
    {
        if (!string.Equals(conversation.Type, "Group", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This operation is only supported for group conversations.");
        }
    }

    private static ConversationSummaryDto MapConversation(Conversation conversation)
    {
        return new ConversationSummaryDto
        {
            Id = conversation.Id,
            Type = conversation.Type,
            Name = conversation.Name,
            AvatarUrl = conversation.AvatarUrl,
            CreatedAt = conversation.CreatedAt,
            LastMessageId = conversation.LastMessageId,
            LastMessageAt = conversation.LastMessageAt,
            LastMessagePreview = BuildMessagePreview(conversation.LastMessage?.Content),
            Members = conversation.Members
                .Where(x => x.LeftAt is null)
                .Select(x => new ConversationMemberDto
                {
                    UserId = x.UserId,
                    Username = x.User.Username,
                    AvatarUrl = x.User.AvatarUrl,
                    Role = x.Role,
                    JoinedAt = x.JoinedAt
                })
                .OrderBy(x => x.Username)
                .ToList()
        };
    }

    private static string? BuildMessagePreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.Trim();
        return trimmed.Length <= 80 ? trimmed : $"{trimmed[..80]}...";
    }
}
