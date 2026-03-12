namespace StudentSocialNetwork.Api.Domain.Entities;

public class MessageReaction
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public string ReactionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
