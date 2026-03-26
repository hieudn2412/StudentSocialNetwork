namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class FollowDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime FollowedAt { get; set; }
}
