using Microsoft.AspNetCore.Http;
using StudentSocialNetwork.Client.Models.Api;

namespace StudentSocialNetwork.Client.Models.ViewModels.Social;

public class AccountLoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccountRegisterViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccountSettingsViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Interests { get; set; }

    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FeedViewModel
{
    public string Tab { get; set; } = "all";
    public string? Search { get; set; }
    public string? Hashtag { get; set; }
    public int? AuthorId { get; set; }
    public IReadOnlyList<PostDto> Posts { get; set; } = Array.Empty<PostDto>();
    public IReadOnlyList<UserDto> Suggestions { get; set; } = Array.Empty<UserDto>();
}

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<PostDto> Posts { get; set; } = Array.Empty<PostDto>();
    public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
}

public class PostCreateViewModel
{
    public string Content { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PostEditViewModel
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PostDetailViewModel
{
    public PostDto? Post { get; set; }
    public IReadOnlyList<CommentDto> Comments { get; set; } = Array.Empty<CommentDto>();
    public string NewComment { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class ProfilePageViewModel
{
    public UserDto? User { get; set; }
    public IReadOnlyList<PostDto> Posts { get; set; } = Array.Empty<PostDto>();
    public IReadOnlyList<FollowDto> Followers { get; set; } = Array.Empty<FollowDto>();
    public IReadOnlyList<FollowDto> Following { get; set; } = Array.Empty<FollowDto>();
    public bool IsCurrentUser { get; set; }
    public bool IsFollowing { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AdminDashboardViewModel
{
    public ReportSummaryDto? Summary { get; set; }
    public IReadOnlyList<TopPostDto> TopPosts { get; set; } = Array.Empty<TopPostDto>();
    public IReadOnlyList<ChartDataDto> PostsByDate { get; set; } = Array.Empty<ChartDataDto>();
    public IReadOnlyList<ChartDataDto> UsersByDate { get; set; } = Array.Empty<ChartDataDto>();
}

public class AdminUsersViewModel
{
    public bool? IsActiveFilter { get; set; }
    public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
}

public class AdminPostsViewModel
{
    public IReadOnlyList<PostDto> Posts { get; set; } = Array.Empty<PostDto>();
}
