namespace StudentSocialNetwork.Api.Application.Hubs;

public class TypingIndicatorDto
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
    public DateTime OccurredAt { get; set; }
}
