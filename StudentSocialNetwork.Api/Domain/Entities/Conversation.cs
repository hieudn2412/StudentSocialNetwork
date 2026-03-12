namespace StudentSocialNetwork.Api.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }
    public string Type { get; set; } = "Direct";
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? LastMessageId { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public User Creator { get; set; } = null!;
    public Message? LastMessage { get; set; }

    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    public ICollection<ConversationSetting> Settings { get; set; } = new List<ConversationSetting>();
}
