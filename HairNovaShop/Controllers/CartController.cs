using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HairNovaShop.Data;
using HairNovaShop.Models;
using HairNovaShop.Services;

namespace HairNovaShop.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ApplicationDbContext _context;

    public CartController(ICartService cartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    public IActionResult Index()
    {
        var cartItems = _cartService.GetCartItems(HttpContext.Session);
        
        // Load product details for each cart item
        var cartItemsWithProducts = new List<CartItem>();
        foreach (var item in cartItems)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == item.ProductId);

            if (product != null)
            {
                cartItemsWithProducts.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.MainImage ?? "/images/placeholder.png",
                    Price = product.Price,
                    Quantity = item.Quantity,
                    Capacity = item.Capacity
                });
            }
        }

        // Load recommended products
        var recommendedProducts = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToList();

        ViewBag.CartItems = cartItemsWithProducts;
        ViewBag.RecommendedProducts = recommendedProducts;

        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1, string? capacity = null)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null || !product.IsActive)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng bán." });
        }

        // Kiểm tra nếu sản phẩm có variants (dung tích) thì bắt buộc phải chọn
        if (!string.IsNullOrEmpty(product.StockByCapacity))
        {
            try
            {
                var variants = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(product.StockByCapacity) ?? new List<Dictionary<string, object>>();
                if (variants.Any() && string.IsNullOrEmpty(capacity))
                {
                    return Json(new { success = false, message = "Vui lòng chọn dung tích sản phẩm.", requiresCapacity = true });
                }
                
                // Kiểm tra dung tích có tồn tại và còn hàng không
                if (!string.IsNullOrEmpty(capacity))
                {
                    var selectedVariant = variants.FirstOrDefault(v => 
                        v?.GetValueOrDefault("Capacity")?.ToString() == capacity);
                    
                    if (selectedVariant == null)
                    {
                        return Json(new { success = false, message = "Dung tích không hợp lệ." });
                    }
                    
                    var stock = selectedVariant.GetValueOrDefault("Stock")?.ToString();
                    var variantStock = int.TryParse(stock, out var s) ? s : 0;
                    if (variantStock < quantity)
                    {
                        return Json(new { success = false, message = $"Số lượng còn lại: {variantStock}. Vui lòng chọn số lượng phù hợp." });
                    }
                }
            }
            catch { }
        }

        _cartService.AddToCart(HttpContext.Session, id, quantity, capacity);
        var cartCount = _cartService.GetCartItemCount(HttpContext.Session);

        return Json(new { success = true, cartCount = cartCount, message = "Đã thêm sản phẩm vào giỏ hàng!" });
    }

    [HttpGet]
    public async Task<IActionResult> GetProductVariants(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại." });
        }

        var variants = new List<object>();
        if (!string.IsNullOrEmpty(product.StockByCapacity))
        {
            try
            {
                var variantsData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(product.StockByCapacity) ?? new List<Dictionary<string, object>>();
                variants = variantsData.Select(v => new
                {
                    Capacity = v?.GetValueOrDefault("Capacity")?.ToString() ?? "",
                    Price = v?.GetValueOrDefault("Price")?.ToString(),
                    Stock = int.TryParse(v?.GetValueOrDefault("Stock")?.ToString() ?? "0", out var s) ? s : 0
                }).Where(v => !string.IsNullOrEmpty(v.Capacity) && v.Stock > 0).ToList<object>();
            }
            catch { }
        }

        return Json(new { success = true, hasVariants = variants.Any(), variants = variants, basePrice = product.Price });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Update(int id, int quantity, string? capacity = null)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
        }

        _cartService.UpdateQuantity(HttpContext.Session, id, quantity, capacity);
        var cartCount = _cartService.GetCartItemCount(HttpContext.Session);

        return Json(new { success = true, cartCount = cartCount });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Remove(int id, string? capacity = null)
    {
        _cartService.RemoveFromCart(HttpContext.Session, id, capacity);
        var cartCount = _cartService.GetCartItemCount(HttpContext.Session);

        return Json(new { success = true, cartCount = cartCount });
    }

    [HttpGet]
    public IActionResult GetCount()
    {
        var cartCount = _cartService.GetCartItemCount(HttpContext.Session);
        return Json(new { count = cartCount });
    }
}
