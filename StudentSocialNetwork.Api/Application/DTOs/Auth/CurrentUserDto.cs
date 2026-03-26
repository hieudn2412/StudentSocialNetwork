using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.DTOs.Auth;

public class CurrentUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Interests { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string AccountProvider { get; set; } = "Email";
    public IReadOnlyCollection<string> ConnectedProviders { get; set; } = Array.Empty<string>();
}
