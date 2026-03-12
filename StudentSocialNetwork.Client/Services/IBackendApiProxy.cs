using System.Net.Http;

namespace StudentSocialNetwork.Client.Services;

public interface IBackendApiProxy
{
    Task<ProxyResult> ForwardAsync(
        HttpMethod method,
        string relativePath,
        string? bearerToken = null,
        object? body = null,
        CancellationToken cancellationToken = default);

    Task<ProxyResult> ForwardContentAsync(
        HttpMethod method,
        string relativePath,
        HttpContent content,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}

public sealed record ProxyResult(int StatusCode, string Content, string ContentType);
