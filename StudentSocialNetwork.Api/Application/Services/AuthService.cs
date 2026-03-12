using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Security;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalAccountRepository _externalAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenLifetimeProvider _refreshTokenLifetimeProvider;

    public AuthService(
        IUserRepository userRepository,
        IExternalAccountRepository externalAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenLifetimeProvider refreshTokenLifetimeProvider)
    {
        _userRepository = userRepository;
        _externalAccountRepository = externalAccountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenLifetimeProvider = refreshTokenLifetimeProvider;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                Username = string.IsNullOrWhiteSpace(request.Username)
                    ? BuildUsernameFromEmail(email)
                    : request.Username.Trim(),
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow,
                Status = "Online"
            };

            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            user.LastActiveAt = DateTime.UtcNow;
            user.Status = "Online";
            _userRepository.Update(user);
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var provider = request.Provider.Trim();
        var providerUserId = request.ProviderUserId.Trim();
        var email = request.Email.Trim();

        var externalAccount = await _externalAccountRepository.GetByProviderAsync(provider, providerUserId, cancellationToken);

        User user;
        if (externalAccount is not null)
        {
            user = externalAccount.User;
            externalAccount.AccessToken = request.AccessToken;
            user.LastActiveAt = DateTime.UtcNow;
            user.Status = "Online";

            _userRepository.Update(user);
            await _externalAccountRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user = await _userRepository.GetByEmailAsync(email, cancellationToken)
                ?? new User
                {
                    Email = email,
                    Username = string.IsNullOrWhiteSpace(request.Username)
                        ? BuildUsernameFromEmail(email)
                        : request.Username.Trim(),
                    AvatarUrl = request.AvatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow,
                    Status = "Online"
                };

            if (user.Id == 0)
            {
                await _userRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                user.LastActiveAt = DateTime.UtcNow;
                user.Status = "Online";
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    user.AvatarUrl = request.AvatarUrl;
                }

                _userRepository.Update(user);
            }

            externalAccount = new ExternalAccount
            {
                Provider = provider,
                ProviderUserId = providerUserId,
                AccessToken = request.AccessToken,
                CreatedAt = DateTime.UtcNow,
                User = user
            };

            await _externalAccountRepository.AddAsync(externalAccount, cancellationToken);
            await _externalAccountRepository.SaveChangesAsync(cancellationToken);
        }

        return await BuildAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenHasher.Hash(request.RefreshToken.Trim());
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid.");

        if (existingRefreshToken.RevokedAt.HasValue || existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");
        }

        var nextRefreshTokenPlain = _refreshTokenGenerator.Generate();
        var nextRefreshTokenHash = _refreshTokenHasher.Hash(nextRefreshTokenPlain);

        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        existingRefreshToken.ReplacedByTokenHash = nextRefreshTokenHash;
        _refreshTokenRepository.Update(existingRefreshToken);

        var newRefreshToken = new RefreshToken
        {
            UserId = existingRefreshToken.UserId,
            TokenHash = nextRefreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeProvider.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        var user = existingRefreshToken.User;
        user.LastActiveAt = DateTime.UtcNow;
        user.Status = "Online";
        _userRepository.Update(user);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var jwtToken = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = jwtToken.AccessToken,
            ExpiresAt = jwtToken.ExpiresAt,
            RefreshToken = nextRefreshTokenPlain,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        };
    }

    public async Task LogoutAsync(int userId, LogoutRequestDto request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = _refreshTokenHasher.Hash(request.RefreshToken.Trim());
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (token is not null && token.UserId == userId && !token.RevokedAt.HasValue && token.ExpiresAt > now)
            {
                token.RevokedAt = now;
                _refreshTokenRepository.Update(token);
            }

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            _refreshTokenRepository.Update(token);
        }

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserDto> GetMeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return await BuildCurrentUserDtoAsync(user, cancellationToken);
    }

    private async Task<CurrentUserDto> BuildCurrentUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var externalAccounts = await _externalAccountRepository.GetByUserIdAsync(user.Id, cancellationToken);

        var connectedProviders = externalAccounts
            .Select(x => NormalizeProviderName(x.Provider))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var accountProvider = connectedProviders.FirstOrDefault() ?? "Email";

        return new CurrentUserDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastActiveAt = user.LastActiveAt,
            AccountProvider = accountProvider,
            ConnectedProviders = connectedProviders
        };
    }

    private static string NormalizeProviderName(string provider)
    {
        return provider.Trim().ToLowerInvariant() switch
        {
            "google" => "Google",
            "github" => "GitHub",
            "facebook" => "Facebook",
            _ => provider
        };
    }
    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        await RevokeActiveRefreshTokensAsync(user.Id, cancellationToken);

        var refreshTokenPlain = _refreshTokenGenerator.Generate();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshTokenPlain);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeProvider.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var jwtToken = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = jwtToken.AccessToken,
            ExpiresAt = jwtToken.ExpiresAt,
            RefreshToken = refreshTokenPlain,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }

    private async Task RevokeActiveRefreshTokensAsync(int userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            _refreshTokenRepository.Update(token);
        }
    }

    private static string BuildUsernameFromEmail(string email)
    {
        var localPart = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            return $"user_{Guid.NewGuid():N}"[..12];
        }

        return localPart.Length <= 100 ? localPart : localPart[..100];
    }
}




