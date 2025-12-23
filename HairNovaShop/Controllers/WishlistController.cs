using HairNovaShop.Data;
using HairNovaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairNovaShop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: /Wishlist/Toggle
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng chức năng yêu thích", requireLogin = true });
            }

            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                // Remove from wishlist
                _context.Wishlists.Remove(existingItem);
                await _context.SaveChangesAsync();
                
                var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
                return Json(new { success = true, added = false, message = "Đã xóa khỏi danh sách yêu thích", count });
            }
            else
            {
                // Add to wishlist
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.Now
                };

                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();
                
                var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
                return Json(new { success = true, added = true, message = "Đã thêm vào danh sách yêu thích", count });
            }
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích", count });
        }

        // GET: /Wishlist/Count
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count });
        }

        // GET: /Wishlist/Check
        [HttpGet]
        public async Task<IActionResult> Check(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { isInWishlist = false });
            }

            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            return Json(new { isInWishlist = exists });
        }

        // GET: /Wishlist/GetUserWishlist - Returns list of product IDs in user's wishlist
        [HttpGet]
        public async Task<IActionResult> GetUserWishlist()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { productIds = new int[0] });
            }

            var productIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return Json(new { productIds });
        }
    }
}
