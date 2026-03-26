using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("post/{postId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommentDto>>>> GetCommentsByPost(Guid postId, CancellationToken cancellationToken)
    {
        var comments = await _commentService.GetCommentsByPostAsync(postId, cancellationToken);
        return Ok(ApiResponse.Ok(comments));
    }

    [Authorize]
    [HttpPost("post/{postId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CommentDto>>> AddComment(
        Guid postId,
        [FromBody] CreateCommentDto request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.AddCommentAsync(postId, GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(comment, "Đã thêm bình luận."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CommentDto>>> UpdateComment(
        Guid id,
        [FromBody] CreateCommentDto request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.UpdateCommentAsync(id, GetCurrentUserId(), IsAdmin(), request, cancellationToken);
        return Ok(ApiResponse.Ok(comment, "Đã cập nhật bình luận."));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(Guid id, CancellationToken cancellationToken)
    {
        await _commentService.DeleteCommentAsync(id, GetCurrentUserId(), IsAdmin(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Đã xoá bình luận."));
    }

    private int GetCurrentUserId()
    {
        var subjectClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(subjectClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}
