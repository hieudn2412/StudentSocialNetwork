using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Models.Api;
using StudentSocialNetwork.Client.Models.ViewModels.Social;
using StudentSocialNetwork.Client.Services.Social;

namespace StudentSocialNetwork.Client.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var vm = new AdminDashboardViewModel
        {
            Summary = await _adminService.GetSummaryAsync(cancellationToken),
            TopPosts = await _adminService.GetTopPostsAsync(cancellationToken),
            PostsByDate = await _adminService.GetPostsByDateAsync(cancellationToken),
            UsersByDate = await _adminService.GetUsersByDateAsync(cancellationToken)
        };

        return View(vm);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports(CancellationToken cancellationToken)
    {
        var vm = new AdminDashboardViewModel
        {
            Summary = await _adminService.GetSummaryAsync(cancellationToken),
            TopPosts = await _adminService.GetTopPostsAsync(cancellationToken),
            PostsByDate = await _adminService.GetPostsByDateAsync(cancellationToken),
            UsersByDate = await _adminService.GetUsersByDateAsync(cancellationToken)
        };

        return View(vm);
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var vm = new AdminUsersViewModel
        {
            IsActiveFilter = isActive,
            Users = await _adminService.GetUsersAsync(isActive, cancellationToken)
        };

        return View(vm);
    }

    [HttpPost("users/{id:int}/toggle-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.ToggleActiveAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { isActive });
    }

    [HttpGet("posts")]
    public async Task<IActionResult> Posts(CancellationToken cancellationToken)
    {
        var vm = new AdminPostsViewModel
        {
            Posts = await _adminService.GetPostsAsync(cancellationToken)
        };

        return View(vm);
    }

    [HttpPost("posts/{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePostStatus(Guid id, PostStatus status, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.UpdatePostStatusAsync(id, status, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Posts));
    }

    [HttpPost("posts/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.DeletePostAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Posts));
    }
}
