using HairNovaShop.Data;
using HairNovaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairNovaShop.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Products/Shop
    public async Task<IActionResult> Shop(string? category, string? sort, int page = 1)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Filter by category
        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            query = query.Where(p => p.Category != null && p.Category.Name.Contains(category));
        }

        // Sort
        switch (sort)
        {
            case "priceAsc":
                query = query.OrderBy(p => p.Price);
                break;
            case "priceDesc":
                query = query.OrderByDescending(p => p.Price);
                break;
            case "new":
                query = query.OrderByDescending(p => p.CreatedAt);
                break;
            case "popular":
            default:
                query = query.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount);
                break;
        }

        var products = await query
            .Include(p => p.Category)
            .ToListAsync();
        ViewBag.Category = category ?? "all";
        ViewBag.Sort = sort ?? "popular";
        ViewBag.CurrentPage = page;

        return View(products);
    }

    // GET: Products/Details/{id}
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null || !product.IsActive)
        {
            return NotFound();
        }

        // Parse images JSON
        List<string>? images = null;
        if (!string.IsNullOrEmpty(product.Images))
        {
            try
            {
                images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.Images);
            }
            catch { }
        }

        ViewBag.Images = images ?? new List<string>();

        return View(product);
    }
}
