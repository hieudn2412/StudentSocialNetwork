using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public class CommentService : ApiClientBase, ICommentService
{
    public CommentService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
        : base(httpClientFactory, httpContextAccessor, configuration)
    {
    }

    public async Task<IReadOnlyList<CommentDto>> GetByPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyList<CommentDto>>(
            HttpMethod.Get,
            $"api/comments/post/{postId}",
            authorize: true,
            cancellationToken: cancellationToken) ?? Array.Empty<CommentDto>();
    }

    public Task<CommentDto?> AddAsync(Guid postId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<CommentDto>(HttpMethod.Post, $"api/comments/post/{postId}", request, authorize: true, cancellationToken: cancellationToken);
    }

    public Task<CommentDto?> UpdateAsync(Guid commentId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<CommentDto>(HttpMethod.Put, $"api/comments/{commentId}", request, authorize: true, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(HttpMethod.Delete, $"api/comments/{commentId}", authorize: true, cancellationToken: cancellationToken);
    }
}
