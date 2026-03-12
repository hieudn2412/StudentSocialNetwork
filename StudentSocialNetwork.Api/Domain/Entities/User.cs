namespace StudentSocialNetwork.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string Status { get; set; } = "Offline";
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }

    public ICollection<ExternalAccount> ExternalAccounts { get; set; } = new List<ExternalAccount>();
    public ICollection<Conversation> CreatedConversations { get; set; } = new List<Conversation>();
    public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    public ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    public ICollection<ConversationSetting> ConversationSettings { get; set; } = new List<ConversationSetting>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
