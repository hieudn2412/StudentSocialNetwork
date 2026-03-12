using System.Security.Cryptography;
using System.Text;
using StudentSocialNetwork.Api.Application.Interfaces.Security;

namespace StudentSocialNetwork.Api.Infrastructure.Security;

public class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
