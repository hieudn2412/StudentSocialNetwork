using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<ExternalAccount> ExternalAccounts { get; set; } = new List<ExternalAccount>();
    public ICollection<Conversation> CreatedConversations { get; set; } = new List<Conversation>();
    public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    public ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    public ICollection<ConversationSetting> ConversationSettings { get; set; } = new List<ConversationSetting>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
}
