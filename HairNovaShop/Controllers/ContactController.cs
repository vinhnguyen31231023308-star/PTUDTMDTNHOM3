using Microsoft.AspNetCore.Mvc;

namespace HairNovaShop.Controllers;

public class ContactController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
