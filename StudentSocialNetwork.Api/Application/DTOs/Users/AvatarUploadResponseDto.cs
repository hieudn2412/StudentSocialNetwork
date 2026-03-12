using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.DTOs.Users;

public class AvatarUploadResponseDto
{
    public string AvatarUrl { get; set; } = string.Empty;
    public CurrentUserDto Profile { get; set; } = new();
}
