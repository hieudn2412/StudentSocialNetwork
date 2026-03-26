using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Models.Api;
using StudentSocialNetwork.Client.Models.ViewModels.Social;
using StudentSocialNetwork.Client.Services.Social;

namespace StudentSocialNetwork.Client.Controllers;

[Route("post")]
public class PostController : Controller
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;

    public PostController(IPostService postService, ICommentService commentService)
    {
        _postService = postService;
        _commentService = commentService;
    }

    [Authorize]
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new PostCreateViewModel());
    }

    [Authorize]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var post = await _postService.CreatePostAsync(model.Content, model.Image, cancellationToken);
            if (post is null)
            {
                model.ErrorMessage = "Không thể tạo bài viết.";
                return View(model);
            }

            return RedirectToAction(nameof(Detail), new { id = post.Id });
        }
        catch (ApiClientException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var vm = new PostDetailViewModel();

        try
        {
            vm.Post = await _postService.GetPostDetailAsync(id, cancellationToken);
            vm.Comments = await _commentService.GetByPostAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            vm.ErrorMessage = ex.Message;
        }

        return View(vm);
    }

    [Authorize]
    [HttpPost("{id:guid}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, PostDetailViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _commentService.AddAsync(id, new CreateCommentDto
            {
                Content = model.NewComment
            }, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _postService.GetPostDetailAsync(id, cancellationToken);
            if (post is null)
            {
                return RedirectToAction(nameof(Detail), new { id });
            }

            return View(new PostEditViewModel
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl
            });
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    [Authorize]
    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PostEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _postService.UpdatePostAsync(id, new UpdatePostDto
            {
                Content = model.Content,
                ImageUrl = model.ImageUrl
            }, cancellationToken);

            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (ApiClientException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    [Authorize]
    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _postService.DeletePostAsync(id, cancellationToken);
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index", "Home");
    }
}
