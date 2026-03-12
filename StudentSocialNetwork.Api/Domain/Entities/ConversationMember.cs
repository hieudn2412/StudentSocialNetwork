namespace StudentSocialNetwork.Api.Domain.Entities;

public class ConversationMember
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public long? LastReadMessageId { get; set; }
    public DateTime? MutedUntil { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
    public Message? LastReadMessage { get; set; }
}
