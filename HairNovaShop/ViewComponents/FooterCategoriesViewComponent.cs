using HairNovaShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairNovaShop.ViewComponents;

public class FooterCategoriesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public FooterCategoriesViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(4)
            .ToListAsync();

        return View(categories);
    }
}
