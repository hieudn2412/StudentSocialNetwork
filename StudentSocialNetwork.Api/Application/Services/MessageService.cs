using System.Text;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Messages;
using StudentSocialNetwork.Api.Application.Hubs;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatRealtimePublisher _chatRealtimePublisher;

    public MessageService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IChatRealtimePublisher chatRealtimePublisher)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _chatRealtimePublisher = chatRealtimePublisher;
    }

    public async Task<CursorPagedResultDto<MessageDto>> GetMessagesAsync(int userId, int conversationId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        await EnsureMembershipAsync(userId, conversationId, cancellationToken);

        var pageSize = Math.Clamp(limit <= 0 ? 30 : limit, 1, 100);
        var (cursorCreatedAt, cursorId) = ParseCursor(cursor);

        var messages = await _messageRepository.GetPageByConversationAsync(
            conversationId,
            pageSize + 1,
            cursorCreatedAt,
            cursorId,
            cancellationToken);

        var hasNext = messages.Count > pageSize;
        var currentItems = hasNext ? messages.Take(pageSize).ToList() : messages.ToList();
        var nextCursor = hasNext
            ? BuildCursor(currentItems.Last().CreatedAt, currentItems.Last().Id)
            : null;

        return new CursorPagedResultDto<MessageDto>
        {
            Items = currentItems.Select(MapMessage).ToList(),
            NextCursor = nextCursor,
            Limit = pageSize
        };
    }

    public async Task<MessageDto> SendMessageAsync(int userId, int conversationId, SendMessageRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureMembershipAsync(userId, conversationId, cancellationToken);

        if (request.ReplyToMessageId.HasValue)
        {
            var replyToMessage = await _messageRepository.GetByIdAsync(request.ReplyToMessageId.Value, cancellationToken);
            if (replyToMessage is null || replyToMessage.ConversationId != conversationId)
            {
                throw new NotFoundException("Reply target message does not exist in this conversation.");
            }
        }

        var content = request.Content?.Trim() ?? string.Empty;

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = content,
            MessageType = string.IsNullOrWhiteSpace(request.MessageType) ? "Text" : request.MessageType.Trim(),
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow,
            Attachments = request.Attachments
                .Where(x => !string.IsNullOrWhiteSpace(x.FileUrl))
                .Select(x => new MessageAttachment
                {
                    FileUrl = x.FileUrl.Trim(),
                    FileName = x.FileName.Trim(),
                    FileType = x.FileType.Trim(),
                    FileSize = x.FileSize
                })
                .ToList()
        };

        await _messageRepository.AddAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation not found.");

        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.CreatedAt;

        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var storedMessage = await _messageRepository.GetByIdAsync(message.Id, cancellationToken)
            ?? throw new InvalidOperationException("Message was saved but could not be reloaded.");

        var result = MapMessage(storedMessage);
        await _chatRealtimePublisher.BroadcastMessageCreatedAsync(conversationId, result, cancellationToken);

        return result;
    }

    public async Task<MessageReactionDto> AddReactionAsync(int userId, long messageId, AddMessageReactionRequestDto request, CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        await EnsureMembershipAsync(userId, message.ConversationId, cancellationToken);

        var reactionType = request.ReactionType.Trim();
        var existingReaction = await _messageRepository.GetReactionAsync(messageId, userId, reactionType, cancellationToken);

        var isAdded = false;
        if (existingReaction is null)
        {
            await _messageRepository.AddReactionAsync(new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _messageRepository.SaveChangesAsync(cancellationToken);

            existingReaction = await _messageRepository.GetReactionAsync(messageId, userId, reactionType, cancellationToken)
                ?? throw new InvalidOperationException("Reaction was saved but could not be reloaded.");

            isAdded = true;
        }

        var reactionDto = new MessageReactionDto
        {
            Id = existingReaction.Id,
            UserId = existingReaction.UserId,
            Username = existingReaction.User.Username,
            ReactionType = existingReaction.ReactionType,
            CreatedAt = existingReaction.CreatedAt
        };

        if (isAdded)
        {
            await _chatRealtimePublisher.BroadcastReactionUpdatedAsync(new MessageReactionBroadcastDto
            {
                ConversationId = message.ConversationId,
                MessageId = messageId,
                UserId = existingReaction.UserId,
                Username = existingReaction.User.Username,
                ReactionType = existingReaction.ReactionType,
                IsRemoved = false,
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return reactionDto;
    }

    public async Task RemoveReactionAsync(int userId, long messageId, string reactionType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reactionType))
        {
            throw new ArgumentException("Reaction type is required.");
        }

        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        await EnsureMembershipAsync(userId, message.ConversationId, cancellationToken);

        var existingReaction = await _messageRepository.GetReactionAsync(messageId, userId, reactionType, cancellationToken);
        if (existingReaction is null)
        {
            return;
        }

        _messageRepository.RemoveReaction(existingReaction);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        await _chatRealtimePublisher.BroadcastReactionUpdatedAsync(new MessageReactionBroadcastDto
        {
            ConversationId = message.ConversationId,
            MessageId = messageId,
            UserId = userId,
            Username = existingReaction.User.Username,
            ReactionType = existingReaction.ReactionType,
            IsRemoved = true,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task MarkAsReadAsync(int userId, long messageId, CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        await EnsureMembershipAsync(userId, message.ConversationId, cancellationToken);

        var existingRead = await _messageRepository.GetReadReceiptAsync(messageId, userId, cancellationToken);
        if (existingRead is null)
        {
            await _messageRepository.AddReadReceiptAsync(new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                SeenAt = DateTime.UtcNow
            }, cancellationToken);
        }

        var membership = await _conversationRepository.GetActiveMemberAsync(message.ConversationId, userId, cancellationToken)
            ?? throw new UnauthorizedException("You are not a member of this conversation.");

        if (!membership.LastReadMessageId.HasValue || membership.LastReadMessageId.Value < messageId)
        {
            membership.LastReadMessageId = messageId;
        }

        await _conversationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkConversationAsReadAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        var membership = await _conversationRepository.GetActiveMemberAsync(conversationId, userId, cancellationToken)
            ?? throw new UnauthorizedException("You are not a member of this conversation.");

        var latestMessageId = await _messageRepository.GetLatestMessageIdAsync(conversationId, cancellationToken);
        if (!latestMessageId.HasValue)
        {
            return;
        }

        if (membership.LastReadMessageId.HasValue && membership.LastReadMessageId.Value >= latestMessageId.Value)
        {
            return;
        }

        var unreadMessageIds = await _messageRepository.GetUnreadMessageIdsAsync(
            conversationId,
            userId,
            membership.LastReadMessageId,
            latestMessageId.Value,
            cancellationToken);

        foreach (var unreadMessageId in unreadMessageIds)
        {
            await _messageRepository.AddReadReceiptAsync(new MessageRead
            {
                MessageId = unreadMessageId,
                UserId = userId,
                SeenAt = DateTime.UtcNow
            }, cancellationToken);
        }

        membership.LastReadMessageId = latestMessageId.Value;
        await _conversationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PinnedMessageDto>> GetPinnedMessagesAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        await EnsureMembershipAsync(userId, conversationId, cancellationToken);

        var pinnedMessages = await _messageRepository.GetPinnedMessagesAsync(conversationId, cancellationToken);
        return pinnedMessages.Select(MapPinnedMessage).ToList();
    }

    public async Task PinMessageAsync(int userId, int conversationId, long messageId, CancellationToken cancellationToken = default)
    {
        await EnsureMembershipAsync(userId, conversationId, cancellationToken);

        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        if (message.ConversationId != conversationId)
        {
            throw new InvalidOperationException("Message does not belong to this conversation.");
        }

        var existingPinnedMessage = await _messageRepository.GetPinnedMessageAsync(conversationId, messageId, cancellationToken);
        if (existingPinnedMessage is not null)
        {
            return;
        }

        await _messageRepository.AddPinnedMessageAsync(new PinnedMessage
        {
            ConversationId = conversationId,
            MessageId = messageId,
            PinnedBy = userId,
            PinnedAt = DateTime.UtcNow
        }, cancellationToken);

        await _messageRepository.SaveChangesAsync(cancellationToken);

        await _chatRealtimePublisher.BroadcastPinnedMessageUpdatedAsync(new PinnedMessageBroadcastDto
        {
            ConversationId = conversationId,
            MessageId = messageId,
            UpdatedByUserId = userId,
            IsPinned = true,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task UnpinMessageAsync(int userId, int conversationId, long messageId, CancellationToken cancellationToken = default)
    {
        await EnsureMembershipAsync(userId, conversationId, cancellationToken);

        var pinnedMessage = await _messageRepository.GetPinnedMessageAsync(conversationId, messageId, cancellationToken);
        if (pinnedMessage is null)
        {
            return;
        }

        _messageRepository.RemovePinnedMessage(pinnedMessage);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        await _chatRealtimePublisher.BroadcastPinnedMessageUpdatedAsync(new PinnedMessageBroadcastDto
        {
            ConversationId = conversationId,
            MessageId = messageId,
            UpdatedByUserId = userId,
            IsPinned = false,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsureMembershipAsync(int userId, int conversationId, CancellationToken cancellationToken)
    {
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, userId, cancellationToken);
        if (!isMember)
        {
            throw new UnauthorizedException("You are not a member of this conversation.");
        }
    }

    private static (DateTime? CreatedAt, long? MessageId) ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split('|');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid cursor.");
            }

            var ticks = long.Parse(parts[0]);
            var messageId = long.Parse(parts[1]);

            return (new DateTime(ticks, DateTimeKind.Utc), messageId);
        }
        catch (Exception)
        {
            throw new ArgumentException("Cursor is invalid.");
        }
    }

    private static string BuildCursor(DateTime createdAt, long messageId)
    {
        var raw = $"{createdAt.Ticks}|{messageId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static PinnedMessageDto MapPinnedMessage(PinnedMessage pinnedMessage)
    {
        return new PinnedMessageDto
        {
            Id = pinnedMessage.Id,
            ConversationId = pinnedMessage.ConversationId,
            MessageId = pinnedMessage.MessageId,
            PinnedBy = pinnedMessage.PinnedBy,
            PinnedByUsername = pinnedMessage.PinnedByUser.Username,
            PinnedAt = pinnedMessage.PinnedAt,
            Message = MapMessage(pinnedMessage.Message)
        };
    }

    private static MessageDto MapMessage(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderUsername = message.Sender.Username,
            Content = message.Content,
            MessageType = message.MessageType,
            ReplyToMessageId = message.ReplyToMessageId,
            CreatedAt = message.CreatedAt,
            EditedAt = message.EditedAt,
            DeletedAt = message.DeletedAt,
            Attachments = message.Attachments.Select(x => new MessageAttachmentDto
            {
                Id = x.Id,
                FileUrl = x.FileUrl,
                FileName = x.FileName,
                FileType = x.FileType,
                FileSize = x.FileSize
            }).ToList(),
            Reactions = message.Reactions.Select(x => new MessageReactionDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Username = x.User.Username,
                ReactionType = x.ReactionType,
                CreatedAt = x.CreatedAt
            }).ToList()
        };
    }
}
