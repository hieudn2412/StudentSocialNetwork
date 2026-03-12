using Microsoft.Extensions.Options;
using StudentSocialNetwork.Api.Application.Interfaces.Security;

namespace StudentSocialNetwork.Api.Infrastructure.Security;

public class RefreshTokenLifetimeProvider : IRefreshTokenLifetimeProvider
{
    private readonly JwtOptions _options;

    public RefreshTokenLifetimeProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public int RefreshTokenDays => _options.RefreshTokenDays <= 0 ? 14 : _options.RefreshTokenDays;
}
