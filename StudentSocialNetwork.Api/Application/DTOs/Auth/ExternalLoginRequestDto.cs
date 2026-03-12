namespace StudentSocialNetwork.Api.Application.DTOs.Auth;

public class ExternalLoginRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? AccessToken { get; set; }
}
