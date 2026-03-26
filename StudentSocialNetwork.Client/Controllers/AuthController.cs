using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using StudentSocialNetwork.Client.Models.Api;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private const string ExternalOAuthScheme = "ExternalOAuth";
    private const string OAuthBootstrapCookieName = "ssn.oauth.auth";
    private const string JwtCookieName = "ssn.jwt";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBackendApiProxy _backendApiProxy;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IBackendApiProxy backendApiProxy,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _backendApiProxy = backendApiProxy;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? error = null, [FromQuery] string? returnUrl = "/conversations")
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Login", "Account", new { error, ReturnUrl = returnUrl });
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete(OAuthBootstrapCookieName);
        Response.Cookies.Delete(JwtCookieName);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet("login/google")]
    public IActionResult LoginGoogle([FromQuery] string? returnUrl = null)
    {
        return ChallengeProvider("Google", nameof(GoogleCallback), returnUrl);
    }

    [HttpGet("callback/google")]
    public Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        return HandleOAuthCallbackAsync("Google", returnUrl, cancellationToken);
    }

    private IActionResult ChallengeProvider(string provider, string callbackActionName, string? returnUrl)
    {
        if (!IsGoogleConfigured())
        {
            return RedirectToAction(nameof(Login), new { error = "Google OAuth is not configured." });
        }

        var redirectUri = Url.Action(callbackActionName, "Auth", new { returnUrl })
            ?? "/auth/callback/google";

        var authProperties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return Challenge(authProperties, provider);
    }

    private async Task<IActionResult> HandleOAuthCallbackAsync(string provider, string? returnUrl, CancellationToken cancellationToken)
    {
        var authenticationResult = await HttpContext.AuthenticateAsync(ExternalOAuthScheme);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null)
        {
            return RedirectToAction(nameof(Login), new { error = $"{provider} authentication failed." });
        }

        var principal = authenticationResult.Principal;

        var profile = BuildOAuthProfile(provider, principal);
        await HttpContext.SignOutAsync(ExternalOAuthScheme);

        if (string.IsNullOrWhiteSpace(profile.ProviderUserId))
        {
            return RedirectToAction(nameof(Login), new { error = $"{provider} did not return a provider user id." });
        }

        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            return RedirectToAction(nameof(Login), new { error = $"{provider} did not provide an email address." });
        }

        var backendRequest = new BackendExternalLoginRequest
        {
            Provider = provider,
            ProviderUserId = profile.ProviderUserId,
            Email = profile.Email,
            Username = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            AccessToken = profile.ProviderAccessToken
        };

        ProxyResult proxyResult;

        try
        {
            proxyResult = await _backendApiProxy.ForwardAsync(
                HttpMethod.Post,
                "api/auth/external-login",
                body: backendRequest,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call backend external login endpoint for provider {Provider}.", provider);

            var backendBaseUrl = _configuration["BackendApi:BaseUrl"];
            var errorMessage = string.IsNullOrWhiteSpace(backendBaseUrl)
                ? "Unable to complete OAuth login. Cannot reach backend API."
                : $"Unable to complete OAuth login. Cannot reach backend API at {backendBaseUrl}.";

            return RedirectToAction(nameof(Login), new { error = errorMessage });
        }

        var envelope = TryDeserialize<ApiEnvelope<AuthTokenPayload>>(proxyResult.Content);

        if (proxyResult.StatusCode < 200 || proxyResult.StatusCode > 299 || envelope is null || !envelope.Success || envelope.Data is null)
        {
            var message = envelope?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "OAuth login failed while issuing application token.";
            }

            return RedirectToAction(nameof(Login), new { error = message });
        }

        await SignInApplicationSessionAsync(envelope.Data);
        WriteOAuthBootstrapCookie(envelope.Data);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private static OAuthProfile BuildOAuthProfile(string provider, ClaimsPrincipal principal)
    {
        var providerUserId = GetFirstClaimValue(principal,
            ClaimTypes.NameIdentifier,
            "sub");

        var email = GetFirstClaimValue(principal,
            ClaimTypes.Email,
            "email");

        var displayName = GetFirstClaimValue(principal,
            ClaimTypes.Name,
            "name",
            "preferred_username");

        var avatarUrl = GetFirstClaimValue(principal,
            "urn:google:picture",
            "picture");

        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(email))
        {
            displayName = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            var suffix = string.IsNullOrWhiteSpace(providerUserId)
                ? Guid.NewGuid().ToString("N")[..8]
                : providerUserId.Replace(" ", string.Empty);

            displayName = $"google_{suffix}";
        }

        if (displayName.Length > 100)
        {
            displayName = displayName[..100];
        }

        return new OAuthProfile
        {
            ProviderUserId = providerUserId ?? string.Empty,
            Email = email ?? string.Empty,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            ProviderAccessToken = null
        };
    }

    private bool IsGoogleConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
               !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);
    }

    private async Task SignInApplicationSessionAsync(AuthTokenPayload payload)
    {
        var role = ResolveUserRole(payload.Token);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
            new(ClaimTypes.Name, payload.Username),
            new(ClaimTypes.Email, payload.Email),
            new(ClaimTypes.Role, role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = payload.ExpiresAt.ToUniversalTime()
            });

        Response.Cookies.Append(JwtCookieName, payload.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = payload.ExpiresAt.ToUniversalTime(),
            Path = "/"
        });
    }

    private static UserRole ResolveUserRole(string token)
    {
        if (TryReadJwtPayload(token) is not JsonElement payload)
        {
            return UserRole.Student;
        }

        var claimKeys = new[]
        {
            "role",
            ClaimTypes.Role,
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        foreach (var claimKey in claimKeys)
        {
            if (!payload.TryGetProperty(claimKey, out var claimValue))
            {
                continue;
            }

            if (claimValue.ValueKind is JsonValueKind.String)
            {
                var roleText = claimValue.GetString();
                if (Enum.TryParse<UserRole>(roleText, true, out var roleByName))
                {
                    return roleByName;
                }

                if (int.TryParse(roleText, out var roleNumber) &&
                    Enum.IsDefined(typeof(UserRole), roleNumber))
                {
                    return (UserRole)roleNumber;
                }
            }
            else if (claimValue.ValueKind is JsonValueKind.Number &&
                     claimValue.TryGetInt32(out var roleNumber) &&
                     Enum.IsDefined(typeof(UserRole), roleNumber))
            {
                return (UserRole)roleNumber;
            }
        }

        return UserRole.Student;
    }

    private static JsonElement? TryReadJwtPayload(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return null;
        }

        try
        {
            var payloadBytes = WebEncoders.Base64UrlDecode(parts[1]);
            using var json = JsonDocument.Parse(payloadBytes);
            return json.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private void WriteOAuthBootstrapCookie(AuthTokenPayload payload)
    {
        var bootstrapPayload = new OAuthBootstrapPayload
        {
            UserId = payload.UserId,
            Username = payload.Username,
            Email = payload.Email,
            Token = payload.Token,
            ExpiresAt = payload.ExpiresAt,
            RefreshToken = payload.RefreshToken,
            RefreshTokenExpiresAt = payload.RefreshTokenExpiresAt
        };

        var json = JsonSerializer.Serialize(bootstrapPayload, JsonOptions);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));

        Response.Cookies.Append(OAuthBootstrapCookieName, encoded, new CookieOptions
        {
            HttpOnly = false,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(2)
        });
    }

    private static string? GetFirstClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static T? TryDeserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private sealed class OAuthProfile
    {
        public string ProviderUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? ProviderAccessToken { get; set; }
    }

    private sealed class BackendExternalLoginRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? AccessToken { get; set; }
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class AuthTokenPayload
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }

    private sealed class OAuthBootstrapPayload
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
