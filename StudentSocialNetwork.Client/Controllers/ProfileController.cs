using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Models.ViewModels.Social;
using StudentSocialNetwork.Client.Services.Social;

namespace StudentSocialNetwork.Client.Controllers;

[Route("profile")]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IFollowService _followService;

    public ProfileController(IUserService userService, IPostService postService, IFollowService followService)
    {
        _userService = userService;
        _postService = postService;
        _followService = followService;
    }

    [HttpGet("{id:int?}")]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var currentUserId = TryGetCurrentUserId();
        var targetUserId = id ?? currentUserId;
        if (!targetUserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var vm = new ProfilePageViewModel();

        try
        {
            vm.User = await _userService.GetProfileAsync(targetUserId.Value, cancellationToken);
            vm.Posts = await _userService.GetPostsByUserAsync(targetUserId.Value, cancellationToken);
            vm.Followers = await _followService.GetFollowersAsync(targetUserId.Value, cancellationToken);
            vm.Following = await _followService.GetFollowingAsync(targetUserId.Value, cancellationToken);

            vm.IsCurrentUser = currentUserId.HasValue && currentUserId.Value == targetUserId.Value;
            vm.IsFollowing = currentUserId.HasValue && vm.Followers.Any(x => x.UserId == currentUserId.Value);
        }
        catch (ApiClientException ex)
        {
            vm.ErrorMessage = ex.Message;
        }

        return View(vm);
    }

    [Authorize]
    [HttpPost("{id:int}/follow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFollow(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _followService.ToggleFollowAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    private int? TryGetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    public IActionResult Connections()
    {
        return View();
    }
}
