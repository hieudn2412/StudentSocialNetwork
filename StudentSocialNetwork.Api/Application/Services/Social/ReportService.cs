using Microsoft.EntityFrameworkCore;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Infrastructure.Persistence;

namespace StudentSocialNetwork.Api.Application.Services.Social;

public class ReportService : IReportService
{
    private readonly ChatDbContext _dbContext;

    public ReportService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var totalPosts = await _dbContext.Posts.CountAsync(cancellationToken);
        var totalComments = await _dbContext.Comments.CountAsync(cancellationToken);

        return new ReportSummaryDto
        {
            TotalUsers = totalUsers,
            TotalPosts = totalPosts,
            TotalComments = totalComments
        };
    }

    public async Task<IReadOnlyList<TopPostDto>> GetTopPostsAsync(int take = 5, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 20);

        var posts = await _dbContext.Posts
            .AsNoTracking()
            .Include(x => x.Author)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .OrderByDescending(x => x.Likes.Count)
            .ThenByDescending(x => x.Comments.Count)
            .ThenByDescending(x => x.CreatedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        return posts.Select(x => new TopPostDto
        {
            PostId = x.Id,
            ContentPreview = x.Content.Length <= 120 ? x.Content : $"{x.Content[..120]}...",
            AuthorName = x.Author.Username,
            LikeCount = x.Likes.Count,
            CommentCount = x.Comments.Count,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<ChartDataDto>> GetPostsByDateAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        return await BuildDailyChartAsync(
            _dbContext.Posts.AsNoTracking().Select(x => x.CreatedAt),
            days,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ChartDataDto>> GetUsersByDateAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        return await BuildDailyChartAsync(
            _dbContext.Users.AsNoTracking().Select(x => x.CreatedAt),
            days,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<ChartDataDto>> BuildDailyChartAsync(
        IQueryable<DateTime> source,
        int days,
        CancellationToken cancellationToken)
    {
        var safeDays = Math.Clamp(days, 1, 365);
        var fromDateUtc = DateTime.UtcNow.Date.AddDays(-(safeDays - 1));

        var grouped = await source
            .Where(x => x >= fromDateUtc)
            .GroupBy(x => x.Date)
            .Select(x => new { Date = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var map = grouped.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var result = new List<ChartDataDto>(safeDays);

        for (var i = 0; i < safeDays; i++)
        {
            var date = DateOnly.FromDateTime(fromDateUtc.AddDays(i));
            map.TryGetValue(date, out var count);
            result.Add(new ChartDataDto
            {
                Date = date,
                Count = count
            });
        }

        return result;
    }
}
