using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers.Api;

[ApiController]
public abstract class ProxyApiControllerBase : ControllerBase
{
    private readonly IBackendApiProxy _backendApiProxy;

    protected ProxyApiControllerBase(IBackendApiProxy backendApiProxy)
    {
        _backendApiProxy = backendApiProxy;
    }

    protected Task<ProxyResult> ForwardAsync(
        HttpMethod method,
        string path,
        string? bearerToken = null,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        return _backendApiProxy.ForwardAsync(method, path, bearerToken, body, cancellationToken);
    }

    protected IActionResult AsProxyResponse(ProxyResult proxyResult)
    {
        return new ContentResult
        {
            StatusCode = proxyResult.StatusCode,
            Content = proxyResult.Content,
            ContentType = proxyResult.ContentType
        };
    }

    protected string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        if (authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[prefix.Length..].Trim();
        }

        return null;
    }
}
