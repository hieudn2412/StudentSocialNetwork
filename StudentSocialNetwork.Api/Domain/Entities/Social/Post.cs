using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Domain.Entities.Social;

public class Post
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Hashtags { get; set; } = string.Empty;
    public PostStatus Status { get; set; } = PostStatus.Pending;
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
