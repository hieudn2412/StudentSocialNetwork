using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StudentSocialNetwork.Client.Services;

public class BackendApiProxy : IBackendApiProxy
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<BackendApiProxy> _logger;

    public BackendApiProxy(HttpClient httpClient, ILogger<BackendApiProxy> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<ProxyResult> ForwardAsync(
        HttpMethod method,
        string relativePath,
        string? bearerToken = null,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        HttpContent? content = null;

        if (body is not null)
        {
            var payload = JsonSerializer.Serialize(body, SerializerOptions);
            content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        return ForwardCoreAsync(method, relativePath, bearerToken, content, cancellationToken);
    }

    public Task<ProxyResult> ForwardContentAsync(
        HttpMethod method,
        string relativePath,
        HttpContent content,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        return ForwardCoreAsync(method, relativePath, bearerToken, content, cancellationToken);
    }

    private async Task<ProxyResult> ForwardCoreAsync(
        HttpMethod method,
        string relativePath,
        string? bearerToken,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var baseAddress = _httpClient.BaseAddress
            ?? throw new InvalidOperationException("Backend API base address is not configured.");

        var requestUri = new Uri(baseAddress, NormalizePath(relativePath));
        var bufferedContent = await BufferContentAsync(content, cancellationToken);

        using var request = BuildRequestMessage(method, requestUri, bearerToken, bufferedContent);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (TryGetFollowableRedirectUri(response, requestUri, out var redirectUri))
        {
            using var redirectedRequest = BuildRequestMessage(method, redirectUri, bearerToken, bufferedContent);
            using var redirectedResponse = await _httpClient.SendAsync(redirectedRequest, cancellationToken);

            _logger.LogDebug(
                "Forwarded {Method} {Path} => {StatusCode} (redirected to {RedirectUri})",
                method,
                relativePath,
                (int)redirectedResponse.StatusCode,
                redirectUri);

            return await BuildProxyResultAsync(redirectedResponse, cancellationToken);
        }

        _logger.LogDebug("Forwarded {Method} {Path} => {StatusCode}", method, relativePath, (int)response.StatusCode);
        return await BuildProxyResultAsync(response, cancellationToken);
    }

    private static async Task<ProxyResult> BuildProxyResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ProxyResult((int)response.StatusCode, responseContent, contentType);
    }

    private static HttpRequestMessage BuildRequestMessage(
        HttpMethod method,
        Uri requestUri,
        string? bearerToken,
        BufferedContent? bufferedContent)
    {
        var request = new HttpRequestMessage(method, requestUri);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (bufferedContent is not null)
        {
            request.Content = BuildHttpContent(bufferedContent);
        }

        return request;
    }

    private static HttpContent BuildHttpContent(BufferedContent bufferedContent)
    {
        var content = new ByteArrayContent(bufferedContent.Bytes);

        foreach (var header in bufferedContent.Headers)
        {
            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return content;
    }

    private static async Task<BufferedContent?> BufferContentAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return null;
        }

        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        var headers = content.Headers
            .Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value.ToArray()))
            .ToList();

        return new BufferedContent(bytes, headers);
    }

    private static bool TryGetFollowableRedirectUri(HttpResponseMessage response, Uri originalUri, out Uri redirectUri)
    {
        redirectUri = null!;

        if (!IsRedirectStatusCode(response.StatusCode) || response.Headers.Location is null)
        {
            return false;
        }

        var location = response.Headers.Location;
        redirectUri = location.IsAbsoluteUri ? location : new Uri(originalUri, location);

        if (!string.Equals(originalUri.Host, redirectUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var isSameScheme = string.Equals(originalUri.Scheme, redirectUri.Scheme, StringComparison.OrdinalIgnoreCase);
        var isHttpsUpgrade = string.Equals(originalUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && string.Equals(redirectUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        return isSameScheme || isHttpsUpgrade;
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.StartsWith('/') ? path[1..] : path;
    }

    private sealed record BufferedContent(byte[] Bytes, List<KeyValuePair<string, string[]>> Headers);
}
