using Microsoft.AspNetCore.Mvc;

namespace StudentSocialNetwork.Client.Controllers;

[Route("profile")]
public class ProfileController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
