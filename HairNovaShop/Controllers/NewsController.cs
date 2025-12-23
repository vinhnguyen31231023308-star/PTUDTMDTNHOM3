using Microsoft.AspNetCore.Mvc;

namespace HairNovaShop.Controllers;

public class NewsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
