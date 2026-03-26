using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.DTOs.Users;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalAccountRepository _externalAccountRepository;

    public UserProfileService(
        IUserRepository userRepository,
        IExternalAccountRepository externalAccountRepository)
    {
        _userRepository = userRepository;
        _externalAccountRepository = externalAccountRepository;
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(int userId, UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var normalizedUsername = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw new ArgumentException("Username is required.");
        }

        user.Username = normalizedUsername.Length <= 100
            ? normalizedUsername
            : normalizedUsername[..100];

        var now = DateTime.UtcNow;
        user.UpdatedAt = now;

        var profile = EnsureProfile(user, now);
        profile.FullName = NormalizeNullable(request.FullName, 200);
        profile.Bio = NormalizeNullable(request.Bio, 1000);
        profile.AvatarUrl = NormalizeNullable(request.AvatarUrl, 1000);
        profile.ClassName = NormalizeNullable(request.ClassName, 100);
        profile.Major = NormalizeNullable(request.Major, 150);
        profile.Interests = NormalizeNullable(request.Interests, 2000);
        profile.UpdatedAt = now;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return await BuildCurrentUserDtoAsync(user, cancellationToken);
    }

    public async Task<CurrentUserDto> UpdateAvatarAsync(int userId, string avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            throw new ArgumentException("Avatar URL is required.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var now = DateTime.UtcNow;
        user.UpdatedAt = now;

        var profile = EnsureProfile(user, now);
        profile.AvatarUrl = avatarUrl.Trim();
        profile.UpdatedAt = now;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return await BuildCurrentUserDtoAsync(user, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(int currentUserId, string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<UserSearchResultDto>();
        }

        var users = await _userRepository.SearchAsync(currentUserId, query, limit, cancellationToken);

        return users
            .Select(x => new UserSearchResultDto
            {
                UserId = x.Id,
                Username = x.Username,
                Email = x.Email,
                AvatarUrl = x.Profile?.AvatarUrl
            })
            .ToList();
    }

    private async Task<CurrentUserDto> BuildCurrentUserDtoAsync(Domain.Entities.User user, CancellationToken cancellationToken)
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

    private static UserProfile EnsureProfile(Domain.Entities.User user, DateTime now)
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
