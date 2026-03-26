using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopPostDto>> GetTopPostsAsync(int take = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartDataDto>> GetPostsByDateAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartDataDto>> GetUsersByDateAsync(int days = 30, CancellationToken cancellationToken = default);
}
