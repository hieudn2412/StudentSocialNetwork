using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Application.DTOs.Users;
using StudentSocialNetwork.Api.Application.Interfaces.Repositories;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

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

        var normalizedBio = request.Bio?.Trim();
        user.Bio = string.IsNullOrWhiteSpace(normalizedBio)
            ? null
            : (normalizedBio.Length <= 500 ? normalizedBio : normalizedBio[..500]);

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl.Trim();
        }

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

        user.AvatarUrl = avatarUrl.Trim();

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
                AvatarUrl = x.AvatarUrl
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
}
