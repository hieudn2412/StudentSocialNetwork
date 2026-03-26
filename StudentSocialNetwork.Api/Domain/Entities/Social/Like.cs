using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Domain.Entities.Social;

public class Like
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
