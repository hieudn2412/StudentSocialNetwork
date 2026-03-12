namespace StudentSocialNetwork.Api.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "StudentSocialNetwork.Api";
    public string Audience { get; set; } = "StudentSocialNetwork.Client";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 120;
    public int RefreshTokenDays { get; set; } = 14;
}
