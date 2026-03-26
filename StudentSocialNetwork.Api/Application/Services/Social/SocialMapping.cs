using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Domain.Entities;
using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Application.Services.Social;

internal static partial class SocialMapping
{
    [GeneratedRegex(@"#\w+", RegexOptions.Compiled)]
    private static partial Regex HashtagRegex();

    internal static string ExtractHashtags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var tags = HashtagRegex()
            .Matches(content)
            .Select(x => x.Value.ToLowerInvariant())
            .Distinct()
            .ToList();

        return tags.Count == 0 ? string.Empty : string.Join(",", tags);
    }

    internal static IQueryable<Post> AsVisiblePosts(this IQueryable<Post> query, int? currentUserId, bool isAdmin)
    {
        if (isAdmin)
        {
            return query;
        }

        if (currentUserId.HasValue)
        {
            var me = currentUserId.Value;
            return query.Where(x => x.Status != PostStatus.Rejected || x.AuthorId == me);
        }

        return query.Where(x => x.Status != PostStatus.Rejected);
    }

    internal static PostDto ToPostDto(this Post post, int? currentUserId)
    {
        return new PostDto
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorName = post.Author.Username,
            AvatarUrl = post.Author.Profile?.AvatarUrl,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            Hashtags = post.Hashtags,
            Status = post.Status,
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count,
            IsLikedByMe = currentUserId.HasValue && post.Likes.Any(x => x.UserId == currentUserId.Value),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }

    internal static UserDto ToUserDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            AvatarUrl = user.Profile?.AvatarUrl,
            Bio = user.Profile?.Bio,
            ClassName = user.Profile?.ClassName,
            Major = user.Profile?.Major,
            Interests = user.Profile?.Interests,
            Role = user.Role,
            IsActive = user.IsActive,
            FollowersCount = user.Followers.Count,
            FollowingCount = user.Following.Count,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    internal static CommentDto ToCommentDto(this Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.Author.Username,
            AvatarUrl = comment.Author.Profile?.AvatarUrl,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    internal static FollowDto ToFollowDto(this Follow follow, bool isFollowerSide)
    {
        var user = isFollowerSide ? follow.Follower : follow.Following;
        return new FollowDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.Profile?.FullName,
            AvatarUrl = user.Profile?.AvatarUrl,
            FollowedAt = follow.CreatedAt
        };
    }
}
