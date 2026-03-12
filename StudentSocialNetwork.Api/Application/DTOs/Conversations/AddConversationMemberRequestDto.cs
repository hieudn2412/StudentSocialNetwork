namespace StudentSocialNetwork.Api.Application.DTOs.Conversations;

public class AddConversationMemberRequestDto
{
    public int UserId { get; set; }
    public string Role { get; set; } = "Member";
}
