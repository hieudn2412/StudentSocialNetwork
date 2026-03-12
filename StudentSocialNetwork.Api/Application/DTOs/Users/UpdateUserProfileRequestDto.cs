namespace StudentSocialNetwork.Api.Application.DTOs.Users;

public class UpdateUserProfileRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
