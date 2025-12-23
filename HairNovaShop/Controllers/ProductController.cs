using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HairNovaShop.Data;
using HairNovaShop.Models;
using Microsoft.AspNetCore.Http;

namespace HairNovaShop.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null || !product.IsActive)
        {
            return NotFound();
        }

        // Get related products (same category)
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Id != id && p.CategoryId == product.CategoryId && p.IsActive)
            .OrderByDescending(p => p.Rating)
            .Take(4)
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        // Check if user can review this product
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {
            // Check if user has purchased this product with completed order
            var canReview = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.ProductId == id 
                    && oi.Order != null 
                    && oi.Order.UserId == userId 
                    && oi.Order.Status == "completed");

            ViewBag.CanReview = canReview;

            // Check if user already reviewed this product
            var hasReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == id && r.UserId == userId);
            ViewBag.HasReviewed = hasReviewed;
        }
        else
        {
            ViewBag.CanReview = false;
            ViewBag.HasReviewed = false;
        }

        return View(product);
    }

    // GET: Product/GetReviews/{productId}
    [HttpGet]
    public async Task<IActionResult> GetReviews(int productId)
    {
        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                Id = r.Id,
                UserName = r.User != null ? r.User.FullName : "Khách hàng",
                UserAvatar = r.User != null ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(r.User.FullName ?? "User")}&background=0061E1&color=fff&length=2" : "",
                Rating = r.Rating,
                Comment = r.Comment ?? "",
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(new { success = true, reviews });
    }

    // POST: Product/AddReview
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddReview(int productId, int orderId, int rating, string? comment)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá sản phẩm" });
        }

        // Validate rating
        if (rating < 1 || rating > 5)
        {
            return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
        }

        // Check if product exists
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
        }

        // Check if user has purchased this product with completed order
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.Status == "completed");

        if (order == null)
        {
            return Json(new { success = false, message = "Chỉ có thể đánh giá sản phẩm sau khi đơn hàng hoàn thành" });
        }

        // Check if product is in this order
        var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
        if (orderItem == null)
        {
            return Json(new { success = false, message = "Sản phẩm này không có trong đơn hàng" });
        }

        // Check if user already reviewed this product
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId && r.OrderId == orderId);

        if (existingReview != null)
        {
            // Update existing review
            existingReview.Rating = rating;
            existingReview.Comment = comment;
            existingReview.UpdatedAt = DateTime.Now;
        }
        else
        {
            // Create new review
            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                OrderId = orderId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
        }

        await _context.SaveChangesAsync();

        // Update product rating and review count
        var allReviews = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        if (allReviews.Any())
        {
            product.Rating = allReviews.Average(r => r.Rating);
            product.ReviewCount = allReviews.Count;
        }
        else
        {
            product.Rating = 0;
            product.ReviewCount = 0;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Đánh giá của bạn đã được ghi nhận. Cảm ơn bạn!" });
    }

    // GET: Product/CanReview/{productId}
    [HttpGet]
    public async Task<IActionResult> CanReview(int productId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Json(new { success = false, canReview = false, message = "Vui lòng đăng nhập" });
        }

        // Get all completed orders for this user that contain this product
        var completedOrders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && o.Status == "completed")
            .SelectMany(o => o.OrderItems.Where(oi => oi.ProductId == productId).Select(oi => new
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                CreatedAt = o.CreatedAt
            }))
            .ToListAsync();

        // Check if user already reviewed this product
        var hasReviewed = await _context.Reviews
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

        return Json(new
        {
            success = true,
            canReview = completedOrders.Any(),
            hasReviewed,
            orders = completedOrders.OrderByDescending(o => o.CreatedAt).Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                CreatedAt = o.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList()
        });
    }
}
