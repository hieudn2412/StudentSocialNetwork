namespace StudentSocialNetwork.Api.Application.Common.Models;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string PostsFolder { get; set; } = "student-social/posts";
}
