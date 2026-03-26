using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Domain.Entities.Social;

public class UserProfile
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Interests { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
