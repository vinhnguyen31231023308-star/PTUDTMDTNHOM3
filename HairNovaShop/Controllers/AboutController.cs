using Microsoft.AspNetCore.Mvc;

namespace HairNovaShop.Controllers;

public class AboutController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
