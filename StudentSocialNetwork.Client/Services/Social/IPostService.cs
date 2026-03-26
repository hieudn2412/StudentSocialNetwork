using Microsoft.AspNetCore.Http;
using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Services.Social;

public interface IPostService
{
    Task<IReadOnlyList<PostDto>> GetFeedAsync(
        string tab = "all",
        string? search = null,
        string? hashtag = null,
        int? author = null,
        CancellationToken cancellationToken = default);

    Task<PostDto?> CreatePostAsync(string content, IFormFile? image, CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostDto request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ToggleLikeAsync(Guid id, CancellationToken cancellationToken = default);
}
