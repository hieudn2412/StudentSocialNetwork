namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class MessageDto
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public long? ReplyToMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public IReadOnlyCollection<MessageAttachmentDto> Attachments { get; set; } = Array.Empty<MessageAttachmentDto>();
    public IReadOnlyCollection<MessageReactionDto> Reactions { get; set; } = Array.Empty<MessageReactionDto>();
}
