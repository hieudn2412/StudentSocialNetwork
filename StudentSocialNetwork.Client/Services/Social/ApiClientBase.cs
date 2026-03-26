using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StudentSocialNetwork.Client.Configuration;
using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackendApiOptions _backendApiOptions;

    protected ApiClientBase(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _backendApiOptions = configuration.GetSection(BackendApiOptions.SectionName).Get<BackendApiOptions>()
            ?? throw new InvalidOperationException("BackendApi options are not configured.");
    }

    protected async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        bool authorize = true,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(authorize);
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse<T>(response.StatusCode, content);
    }

    protected async Task<T?> SendMultipartAsync<T>(
        HttpMethod method,
        string path,
        MultipartFormDataContent content,
        bool authorize = true,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(authorize);
        using var request = new HttpRequestMessage(method, path)
        {
            Content = content
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse<T>(response.StatusCode, responseContent);
    }

    private HttpClient CreateClient(bool authorize)
    {
        var client = _httpClientFactory.CreateClient(nameof(BackendApiOptions));
        client.BaseAddress ??= new Uri(_backendApiOptions.BaseUrl, UriKind.Absolute);

        if (authorize)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["ssn.jwt"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return client;
    }

    private static T? ParseResponse<T>(System.Net.HttpStatusCode statusCode, string content)
    {
        ApiEnvelope<T>? envelope = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(content, JsonOptions);
        }

        if ((int)statusCode < 200 || (int)statusCode > 299 || envelope?.Success == false)
        {
            var message = envelope?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"API request failed with status {(int)statusCode}.";
            }

            throw new ApiClientException(message, (int)statusCode);
        }

        return envelope is null ? default : envelope.Data;
    }
}

public class ApiClientException : Exception
{
    public ApiClientException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
