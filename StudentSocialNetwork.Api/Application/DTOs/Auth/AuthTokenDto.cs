using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.DTOs.Auth;

public class AuthTokenDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
