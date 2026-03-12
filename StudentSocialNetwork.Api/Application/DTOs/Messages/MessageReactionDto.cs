namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class MessageReactionDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ReactionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
