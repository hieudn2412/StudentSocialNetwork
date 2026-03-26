using Microsoft.AspNetCore.Identity;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Security;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Domain.Entities.Social;

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
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IExternalAccountRepository externalAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenLifetimeProvider refreshTokenLifetimeProvider,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _externalAccountRepository = externalAccountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenLifetimeProvider = refreshTokenLifetimeProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthTokenDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Username đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = username,
            Email = email,
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.Profile = new UserProfile
        {
            FullName = NormalizeNullable(request.FullName, 200),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return BuildAuthToken(user);
    }

    public async Task<AuthTokenDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khoá");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult is PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return BuildAuthToken(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khoá");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyResult is PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    // Legacy chat-auth endpoints kept for backward compatibility.
    public async Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var provider = request.Provider.Trim();
        var providerUserId = request.ProviderUserId.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var externalAccount = await _externalAccountRepository.GetByProviderAsync(provider, providerUserId, cancellationToken);
        var now = DateTime.UtcNow;

        User user;
        if (externalAccount is not null)
        {
            user = externalAccount.User;
            if (!user.IsActive)
            {
                throw new ForbiddenException("Tài khoản đã bị khoá");
            }

            externalAccount.AccessToken = request.AccessToken;
            user.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                var profile = EnsureProfile(user, now);
                profile.AvatarUrl = request.AvatarUrl.Trim();
                profile.UpdatedAt = now;
            }

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
                    Role = UserRole.Student,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

            if (!user.IsActive)
            {
                throw new ForbiddenException("Tài khoản đã bị khoá");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));
            }

            if (user.Id == 0)
            {
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    user.Profile = new UserProfile
                    {
                        AvatarUrl = request.AvatarUrl.Trim(),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                }

                await _userRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                user.UpdatedAt = now;
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    var profile = EnsureProfile(user, now);
                    profile.AvatarUrl = request.AvatarUrl.Trim();
                    profile.UpdatedAt = now;
                }

                _userRepository.Update(user);
            }

            externalAccount = new ExternalAccount
            {
                Provider = provider,
                ProviderUserId = providerUserId,
                AccessToken = request.AccessToken,
                CreatedAt = now,
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

        var user = existingRefreshToken.User;
        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khoá");
        }

        var nextRefreshTokenPlain = _refreshTokenGenerator.Generate();
        var nextRefreshTokenHash = _refreshTokenHasher.Hash(nextRefreshTokenPlain);

        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        existingRefreshToken.ReplacedByTokenHash = nextRefreshTokenHash;
        _refreshTokenRepository.Update(existingRefreshToken);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = nextRefreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeProvider.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        user.UpdatedAt = DateTime.UtcNow;
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
            FullName = user.Profile?.FullName,
            AvatarUrl = user.Profile?.AvatarUrl,
            Bio = user.Profile?.Bio,
            ClassName = user.Profile?.ClassName,
            Major = user.Profile?.Major,
            Interests = user.Profile?.Interests,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
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

    private AuthTokenDto BuildAuthToken(User user)
    {
        var jwtToken = _jwtTokenGenerator.GenerateToken(user);
        return new AuthTokenDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Token = jwtToken.AccessToken,
            ExpiresAt = jwtToken.ExpiresAt
        };
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

    private static UserProfile EnsureProfile(User user, DateTime now)
    {
        if (user.Profile is not null)
        {
            return user.Profile;
        }

        user.Profile = new UserProfile
        {
            UserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        return user.Profile;
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
