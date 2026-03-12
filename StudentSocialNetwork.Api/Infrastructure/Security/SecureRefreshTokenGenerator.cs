using System.Security.Cryptography;
using StudentSocialNetwork.Api.Application.Interfaces.Security;

namespace StudentSocialNetwork.Api.Infrastructure.Security;

public class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
