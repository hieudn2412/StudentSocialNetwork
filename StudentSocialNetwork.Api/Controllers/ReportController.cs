using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<ReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReportSummaryDto>>> Summary(CancellationToken cancellationToken)
    {
        var summary = await _reportService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse.Ok(summary));
    }

    [HttpGet("top-posts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopPostDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopPostDto>>>> TopPosts([FromQuery] int take = 5, CancellationToken cancellationToken = default)
    {
        var posts = await _reportService.GetTopPostsAsync(take, cancellationToken);
        return Ok(ApiResponse.Ok(posts));
    }

    [HttpGet("posts-by-date")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChartDataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChartDataDto>>>> PostsByDate([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var chart = await _reportService.GetPostsByDateAsync(days, cancellationToken);
        return Ok(ApiResponse.Ok(chart));
    }

    [HttpGet("users-by-date")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChartDataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChartDataDto>>>> UsersByDate([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var chart = await _reportService.GetUsersByDateAsync(days, cancellationToken);
        return Ok(ApiResponse.Ok(chart));
    }
}
