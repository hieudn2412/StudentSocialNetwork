using Microsoft.AspNetCore.Mvc;

namespace StudentSocialNetwork.Client.Controllers;

[Route("settings")]
public class SettingsController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
