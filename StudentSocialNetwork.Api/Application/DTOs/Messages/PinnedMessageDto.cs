namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class PinnedMessageDto
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int PinnedBy { get; set; }
    public string PinnedByUsername { get; set; } = string.Empty;
    public DateTime PinnedAt { get; set; }
    public MessageDto Message { get; set; } = new();
}
