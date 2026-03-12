namespace StudentSocialNetwork.Api.Application.DTOs.Auth;

public class CurrentUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public string AccountProvider { get; set; } = "Email";
    public IReadOnlyCollection<string> ConnectedProviders { get; set; } = Array.Empty<string>();
}
