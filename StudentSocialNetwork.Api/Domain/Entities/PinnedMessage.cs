namespace StudentSocialNetwork.Api.Domain.Entities;

public class PinnedMessage
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int PinnedBy { get; set; }
    public DateTime PinnedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Message Message { get; set; } = null!;
    public User PinnedByUser { get; set; } = null!;
}
