namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class TopPostDto
{
    public Guid PostId { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
