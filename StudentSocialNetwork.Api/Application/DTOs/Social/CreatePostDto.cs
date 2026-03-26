namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class CreatePostDto
{
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
