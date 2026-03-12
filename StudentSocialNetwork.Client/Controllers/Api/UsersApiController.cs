using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers.Api;

[Route("api/users")]
public class UsersApiController : ProxyApiControllerBase
{
    private readonly IBackendApiProxy _backendApiProxy;

    public UsersApiController(IBackendApiProxy backendApiProxy)
        : base(backendApiProxy)
    {
        _backendApiProxy = backendApiProxy;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query.Add($"q={Uri.EscapeDataString(q)}");
        }

        if (limit > 0)
        {
            query.Add($"limit={limit}");
        }

        var suffix = query.Count > 0 ? $"?{string.Join("&", query)}" : string.Empty;

        var proxyResult = await ForwardAsync(
            HttpMethod.Get,
            $"api/users/search{suffix}",
            GetBearerToken(),
            cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Put,
            "api/users/profile",
            GetBearerToken(),
            request,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar, CancellationToken cancellationToken)
    {
        if (avatar is null || avatar.Length <= 0)
        {
            return BadRequest(new { success = false, message = "Avatar file is required.", data = (object?)null });
        }

        await using var stream = avatar.OpenReadStream();

        using var multipartContent = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(avatar.ContentType))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(avatar.ContentType);
        }

        multipartContent.Add(streamContent, "avatar", Path.GetFileName(avatar.FileName));

        var proxyResult = await _backendApiProxy.ForwardContentAsync(
            HttpMethod.Post,
            "api/users/avatar",
            multipartContent,
            GetBearerToken(),
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    public sealed class UpdateProfileRequest
    {
        public string Username { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
