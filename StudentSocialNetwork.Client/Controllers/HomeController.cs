using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Models.ViewModels.Social;
using StudentSocialNetwork.Client.Services.Social;

namespace StudentSocialNetwork.Client.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _postService;
    private readonly IUserService _userService;

    public HomeController(IPostService postService, IUserService userService)
    {
        _postService = postService;
        _userService = userService;
    }

    [HttpGet("/")]
    [HttpGet("/home")]
    [HttpGet("/home/index")]
    public async Task<IActionResult> Index(
        [FromQuery] string tab = "all",
        [FromQuery] string? search = null,
        [FromQuery] string? hashtag = null,
        [FromQuery] int? author = null,
        CancellationToken cancellationToken = default)
    {
        var vm = new FeedViewModel
        {
            Tab = tab,
            Search = search,
            Hashtag = hashtag,
            AuthorId = author
        };

        try
        {
            vm.Posts = await _postService.GetFeedAsync(tab, search, hashtag, author, cancellationToken);

            if (User.Identity?.IsAuthenticated == true)
            {
                vm.Suggestions = await _userService.GetSuggestionsAsync(cancellationToken);
            }
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return View(vm);
    }

    [HttpGet("/search")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string query, CancellationToken cancellationToken = default)
    {
        var vm = new SearchViewModel
        {
            Query = query ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(vm.Query))
        {
            return View(vm);
        }

        try
        {
            vm.Users = await _userService.SearchUsersAsync(vm.Query, 30, cancellationToken);
            vm.Posts = await _postService.GetFeedAsync("all", vm.Query, null, null, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return View(vm);
    }

    [Authorize]
    [HttpPost("/posts/{id:guid}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _postService.ToggleLikeAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/home/trending")]
    public IActionResult Trending()
    {
        return View();
    }
}
