namespace StudentSocialNetwork.Api.Application.Hubs;

public class MessageReactionBroadcastDto
{
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ReactionType { get; set; } = string.Empty;
    public bool IsRemoved { get; set; }
    public DateTime OccurredAt { get; set; }
}
