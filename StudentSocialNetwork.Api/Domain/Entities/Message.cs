namespace StudentSocialNetwork.Api.Domain.Entities;

public class Message
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public long? ReplyToMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public Message? ReplyToMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<ConversationMember> LastReadByMembers { get; set; } = new List<ConversationMember>();
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public ICollection<MessageRead> Reads { get; set; } = new List<MessageRead>();
    public ICollection<PinnedMessage> PinnedByConversations { get; set; } = new List<PinnedMessage>();
}
