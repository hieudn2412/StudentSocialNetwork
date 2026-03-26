using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Interests { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
