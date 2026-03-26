using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class PostService : ApiClientBase, IPostService
{
    public PostService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public async Task<IReadOnlyList<PostDto>> GetFeedAsync(
        string tab = "all",
        string? search = null,
        string? hashtag = null,
        int? author = null,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>
        {
            $"tab={Uri.EscapeDataString(tab)}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(hashtag))
        {
            parts.Add($"hashtag={Uri.EscapeDataString(hashtag)}");
        }

        if (author.HasValue)
        {
            parts.Add($"author={author.Value}");
        }

        var path = $"api/posts?{string.Join("&", parts)}";
        return await SendAsync<IReadOnlyList<PostDto>>(HttpMethod.Get, path, authorize: true, cancellationToken: cancellationToken)
            ?? Array.Empty<PostDto>();
    }

    public async Task<PostDto?> CreatePostAsync(string content, IFormFile? image, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(content), "Content");

        if (image is not null && image.Length > 0)
        {
            var stream = image.OpenReadStream();
            var imageContent = new StreamContent(stream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            form.Add(imageContent, "Image", image.FileName);
        }

        return await SendMultipartAsync<PostDto>(HttpMethod.Post, "api/posts", form, authorize: true, cancellationToken: cancellationToken);
    }

    public Task<PostDto?> GetPostDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return SendAsync<PostDto>(HttpMethod.Get, $"api/posts/{id}", authorize: true, cancellationToken: cancellationToken);
    }

    public Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<PostDto>(HttpMethod.Put, $"api/posts/{id}", request, authorize: true, cancellationToken: cancellationToken);
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(HttpMethod.Delete, $"api/posts/{id}", authorize: true, cancellationToken: cancellationToken);
    }

    public async Task<bool> ToggleLikeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<ToggleLikeResult>(HttpMethod.Post, $"api/posts/{id}/like", authorize: true, cancellationToken: cancellationToken);
        return result?.Liked ?? false;
    }

    private sealed class ToggleLikeResult
    {
        public bool Liked { get; set; }
    }
}
