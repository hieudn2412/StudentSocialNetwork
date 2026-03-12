namespace StudentSocialNetwork.Api.Application.Hubs;

public class PinnedMessageBroadcastDto
{
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int UpdatedByUserId { get; set; }
    public bool IsPinned { get; set; }
    public DateTime OccurredAt { get; set; }
}
