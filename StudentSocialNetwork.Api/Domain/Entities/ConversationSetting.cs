namespace StudentSocialNetwork.Api.Domain.Entities;

public class ConversationSetting
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public bool IsMuted { get; set; }
    public bool IsArchived { get; set; }
    public string? Theme { get; set; }
    public string? NotificationLevel { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
