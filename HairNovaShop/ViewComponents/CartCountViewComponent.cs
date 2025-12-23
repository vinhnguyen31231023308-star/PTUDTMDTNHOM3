using HairNovaShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace HairNovaShop.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartCountViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public IViewComponentResult Invoke()
    {
        var count = _cartService.GetCartItemCount(HttpContext.Session);
        return View(count);
    }
}
