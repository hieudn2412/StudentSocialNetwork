namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
