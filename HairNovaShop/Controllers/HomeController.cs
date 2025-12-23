using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HairNovaShop.Models;
using HairNovaShop.Data;

namespace HairNovaShop.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Load all featured products for filtering by category
        var allFeaturedProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsFeatured && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var newProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsNew && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync();

        var onSaleProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.OnSale && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Load all active categories for tabs and category section
        var allCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.AllFeaturedProducts = allFeaturedProducts;
        ViewBag.NewProducts = newProducts;
        ViewBag.OnSaleProducts = onSaleProducts;
        ViewBag.Categories = allCategories.Take(4).ToList(); // For category section
        ViewBag.AllCategories = allCategories; // For product tabs

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
