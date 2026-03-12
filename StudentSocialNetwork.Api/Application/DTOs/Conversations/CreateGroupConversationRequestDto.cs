namespace StudentSocialNetwork.Api.Application.DTOs.Conversations;

public class CreateGroupConversationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
