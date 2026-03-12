namespace StudentSocialNetwork.Api.Application.Interfaces.Security;

public interface IRefreshTokenLifetimeProvider
{
    int RefreshTokenDays { get; }
}
