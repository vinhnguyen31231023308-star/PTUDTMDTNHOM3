using Microsoft.AspNetCore.Mvc;

namespace HairNovaShop.Controllers;

public class PolicyController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
