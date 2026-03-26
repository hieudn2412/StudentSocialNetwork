using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Domain.Entities.Social;

public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}
