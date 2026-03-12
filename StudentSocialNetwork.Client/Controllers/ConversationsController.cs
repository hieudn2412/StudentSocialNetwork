using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudentSocialNetwork.Client.Configuration;
using StudentSocialNetwork.Client.Models.ViewModels;

namespace StudentSocialNetwork.Client.Controllers;

[Route("conversations")]
public class ConversationsController : Controller
{
    private readonly BackendApiOptions _backendApiOptions;

    public ConversationsController(IOptions<BackendApiOptions> backendApiOptions)
    {
        _backendApiOptions = backendApiOptions.Value;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(BuildModel(null));
    }

    [HttpGet("chat/{conversationId:int?}")]
    public IActionResult Chat(int? conversationId)
    {
        return View(BuildModel(conversationId));
    }

    private ChatPageViewModel BuildModel(int? conversationId)
    {
        return new ChatPageViewModel
        {
            ConversationId = conversationId,
            BackendBaseUrl = _backendApiOptions.BaseUrl.TrimEnd('/'),
            HubEndpoint = _backendApiOptions.HubPath
        };
    }
}
