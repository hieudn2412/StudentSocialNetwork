namespace StudentSocialNetwork.Api.Domain.Entities;

public class ExternalAccount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
