namespace StudentSocialNetwork.Api.Application.DTOs.Social;

public class UpdatePostDto
{
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
