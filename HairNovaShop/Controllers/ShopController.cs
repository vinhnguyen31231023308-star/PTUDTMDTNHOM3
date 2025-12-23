using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HairNovaShop.Data;
using HairNovaShop.Models;

namespace HairNovaShop.Controllers;

public class ShopController : Controller
{
    private readonly ApplicationDbContext _context;

    public ShopController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category, string? brand, string? sort = "popular", int page = 1, decimal? maxPrice = null, string? search = null)
    {
        // Get maximum price from all active products
        var maxProductPrice = await _context.Products
            .Where(p => p.IsActive)
            .MaxAsync(p => (decimal?)p.Price) ?? 1000000;

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Search functionality - search in name, description, brand, and tags
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(p => 
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.DetailedDescription != null && p.DetailedDescription.ToLower().Contains(searchTerm)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                (p.Tags != null && p.Tags.ToLower().Contains(searchTerm))
            );
        }

        // Filter by category name
        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            query = query.Where(p => p.Category != null && p.Category.Name == category);
        }

        // Filter by brand
        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(p => p.Brand == brand);
        }

        // Filter by price
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Sort
        query = sort switch
        {
            "new" => query.OrderByDescending(p => p.CreatedAt),
            "priceAsc" => query.OrderBy(p => p.Price),
            "priceDesc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount) // popular
        };

        var totalProducts = await query.CountAsync();
        var pageSize = 16;
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

        var products = await query
            .Include(p => p.Category)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CategoryName = category ?? "all";
        ViewBag.Brand = brand;
        ViewBag.Sort = sort;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.MaxPrice = maxPrice ?? maxProductPrice;
        ViewBag.MaxProductPrice = maxProductPrice;
        ViewBag.SearchTerm = search;

        // Get categories for filter sidebar
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Get distinct brands for filter
        ViewBag.Brands = await _context.Products
            .Where(p => !string.IsNullOrEmpty(p.Brand) && p.IsActive)
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string query, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(new List<object>());
        }

        var searchTerm = query.Trim().ToLower();

        var suggestions = await _context.Products
            .Where(p => p.IsActive && (
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                (p.Tags != null && p.Tags.ToLower().Contains(searchTerm))
            ))
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.ReviewCount)
            .Take(limit)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                brand = p.Brand ?? "",
                price = p.Price,
                image = p.MainImage ?? ""
            })
            .ToListAsync();

        return Json(suggestions);
    }
}
