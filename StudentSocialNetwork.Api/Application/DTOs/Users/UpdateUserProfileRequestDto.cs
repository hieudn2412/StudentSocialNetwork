namespace StudentSocialNetwork.Api.Application.DTOs.Users;

public class UpdateUserProfileRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Interests { get; set; }
}
