using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers.Api;

[Route("api/auth")]
public class AuthApiController : ProxyApiControllerBase
{
    public AuthApiController(IBackendApiProxy backendApiProxy)
        : base(backendApiProxy)
    {
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(HttpMethod.Post, "api/auth/refresh-token", body: request, cancellationToken: cancellationToken);
        return AsProxyResponse(proxyResult);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            "api/auth/logout",
            GetBearerToken(),
            request,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(HttpMethod.Get, "api/auth/me", GetBearerToken(), cancellationToken: cancellationToken);
        return AsProxyResponse(proxyResult);
    }

    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }
}
