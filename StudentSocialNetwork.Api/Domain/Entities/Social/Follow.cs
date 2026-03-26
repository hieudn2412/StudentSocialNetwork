using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Domain.Entities.Social;

public class Follow
{
    public Guid Id { get; set; }
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Follower { get; set; } = null!;
    public User Following { get; set; } = null!;
}
