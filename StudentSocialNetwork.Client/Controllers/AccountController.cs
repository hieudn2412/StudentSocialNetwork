using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Models.Api;
using StudentSocialNetwork.Client.Models.ViewModels.Social;
using StudentSocialNetwork.Client.Services.Social;

namespace StudentSocialNetwork.Client.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private const string JwtCookieName = "ssn.jwt";

    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AccountController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? error = null, [FromQuery(Name = "ReturnUrl")] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        return View(new AccountLoginViewModel
        {
            ErrorMessage = string.IsNullOrWhiteSpace(error) ? null : error,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AccountLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var token = await _authService.LoginAsync(new LoginDto
            {
                Email = model.Email,
                Password = model.Password
            }, cancellationToken);

            if (token is null)
            {
                model.ErrorMessage = "Đăng nhập thất bại.";
                return View(model);
            }

            await SignInAsync(token);
            return RedirectToAction("Index", "Home");
        }
        catch (ApiClientException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new AccountRegisterViewModel());
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AccountRegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var token = await _authService.RegisterAsync(new RegisterDto
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password,
                FullName = model.FullName
            }, cancellationToken);

            if (token is null)
            {
                model.ErrorMessage = "Đăng ký thất bại.";
                return View(model);
            }

            await SignInAsync(token);
            return RedirectToAction("Index", "Home");
        }
        catch (ApiClientException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete(JwtCookieName);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _userService.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new AccountSettingsViewModel
        {
            UserId = profile.Id,
            Username = profile.Username,
            Email = profile.Email,
            FullName = profile.FullName,
            AvatarUrl = profile.AvatarUrl,
            Bio = profile.Bio,
            ClassName = profile.ClassName,
            Major = profile.Major,
            Interests = profile.Interests
        });
    }

    [Authorize]
    [HttpPost("settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(AccountSettingsViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            var updated = await _userService.UpdateProfileAsync(userId, new UpdateProfileDto
            {
                Username = model.Username,
                FullName = model.FullName,
                AvatarUrl = model.AvatarUrl,
                Bio = model.Bio,
                ClassName = model.ClassName,
                Major = model.Major,
                Interests = model.Interests
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(model.CurrentPassword) && !string.IsNullOrWhiteSpace(model.NewPassword))
            {
                await _authService.ChangePasswordAsync(new ChangePasswordDto
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword
                }, cancellationToken);
            }

            model.Message = "Cập nhật thành công.";
            model.ErrorMessage = null;
            model.CurrentPassword = null;
            model.NewPassword = null;

            if (updated is not null)
            {
                model.UserId = updated.Id;
                model.Username = updated.Username;
                model.Email = updated.Email;
                model.FullName = updated.FullName;
                model.AvatarUrl = updated.AvatarUrl;
                model.Bio = updated.Bio;
                model.ClassName = updated.ClassName;
                model.Major = updated.Major;
                model.Interests = updated.Interests;
            }

            return View(model);
        }
        catch (ApiClientException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    private async Task SignInAsync(AuthTokenDto token)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.UserId.ToString()),
            new(ClaimTypes.Name, token.Username),
            new(ClaimTypes.Email, token.Email),
            new(ClaimTypes.Role, token.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = token.ExpiresAt.ToUniversalTime()
            });

        Response.Cookies.Append(JwtCookieName, token.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = token.ExpiresAt.ToUniversalTime(),
            Path = "/"
        });
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin đăng nhập.");
        }

        return userId;
    }
}
