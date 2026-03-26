using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Social;
using StudentSocialNetwork.Api.Application.Interfaces.Services;
using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetAllUsers([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var users = await _adminService.GetAllUsersAsync(isActive, cancellationToken);
        return Ok(ApiResponse.Ok(users));
    }

    [HttpPut("users/{id:int}/toggle-active")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDto>>> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var user = await _adminService.ToggleActiveAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok(user, "Cập nhật trạng thái tài khoản thành công."));
    }

    [HttpGet("posts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PostDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PostDto>>>> GetAllPosts(CancellationToken cancellationToken)
    {
        var posts = await _adminService.GetAllPostsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(posts));
    }

    [HttpPut("posts/{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PostDto>>> UpdatePostStatus(
        Guid id,
        [FromBody] UpdatePostStatusRequest request,
        CancellationToken cancellationToken)
    {
        var post = await _adminService.UpdatePostStatusAsync(id, request.Status, cancellationToken);
        return Ok(ApiResponse.Ok(post, "Cập nhật trạng thái bài viết thành công."));
    }

    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeletePostAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Đã xoá bài viết."));
    }

    public sealed class UpdatePostStatusRequest
    {
        public PostStatus Status { get; set; }
    }
}
