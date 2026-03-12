namespace StudentSocialNetwork.Api.Application.DTOs.Conversations;

public class CreateConversationRequestDto
{
    public string Type { get; set; } = "Direct";
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
