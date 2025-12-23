using System.Security.Cryptography;
using System.Text;
using HairNovaShop.Attributes;
using HairNovaShop.Data;
using HairNovaShop.Helpers;
using HairNovaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace HairNovaShop.Controllers;

[AuthorizeAdmin]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AdminController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: Admin
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        // Statistics
        var totalCustomers = await _context.Users.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == "completed")
            .SumAsync(o => (decimal?)o.Total) ?? 0;

        // Month statistics
        var newCustomersThisMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= thisMonth);
        var newCustomersLastMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= lastMonth && u.CreatedAt < thisMonth);
        var customerGrowth = newCustomersLastMonth > 0
            ? Math.Round((double)(newCustomersThisMonth - newCustomersLastMonth) / newCustomersLastMonth * 100, 1)
            : (newCustomersThisMonth > 0 ? 100 : 0);

        var ordersThisMonth = await _context.Orders
            .CountAsync(o => o.CreatedAt >= thisMonth);
        var ordersLastMonth = await _context.Orders
            .CountAsync(o => o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth);
        var orderGrowth = ordersLastMonth > 0
            ? Math.Round((double)(ordersThisMonth - ordersLastMonth) / ordersLastMonth * 100, 1)
            : (ordersThisMonth > 0 ? 100 : 0);

        var revenueThisMonth = await _context.Orders
            .Where(o => o.Status == "completed" && o.CreatedAt >= thisMonth)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
        var revenueLastMonth = await _context.Orders
            .Where(o => o.Status == "completed" && o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
        var revenueGrowth = revenueLastMonth > 0
            ? Math.Round((double)(revenueThisMonth - revenueLastMonth) / (double)revenueLastMonth * 100, 1)
            : (revenueThisMonth > 0 ? 100 : 0);

        var productsThisMonth = await _context.Products
            .CountAsync(p => p.CreatedAt >= thisMonth);
        var productsLastMonth = await _context.Products
            .CountAsync(p => p.CreatedAt >= lastMonth && p.CreatedAt < thisMonth);
        var productGrowth = productsLastMonth > 0
            ? Math.Round((double)(productsThisMonth - productsLastMonth) / productsLastMonth * 100, 1)
            : (productsThisMonth > 0 ? 100 : 0);

        // Recent orders (last 10)
        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new
            {
                o.Id,
                o.OrderCode,
                CustomerName = o.CustomerName,
                o.CreatedAt,
                o.Total,
                o.Status
            })
            .ToListAsync();

        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.CustomerGrowth = customerGrowth;
        ViewBag.OrderGrowth = orderGrowth;
        ViewBag.RevenueGrowth = revenueGrowth;
        ViewBag.ProductGrowth = productGrowth;
        ViewBag.NewCustomersThisMonth = newCustomersThisMonth;
        ViewBag.OrdersThisMonth = ordersThisMonth;
        ViewBag.RevenueThisMonth = revenueThisMonth;
        ViewBag.ProductsThisMonth = productsThisMonth;
        ViewBag.RecentOrders = recentOrders.Select(o => new Dictionary<string, object>
        {
            { "Id", o.Id },
            { "OrderCode", o.OrderCode },
            { "CustomerName", o.CustomerName },
            { "CreatedAt", o.CreatedAt },
            { "Total", o.Total },
            { "Status", o.Status }
        }).ToList();

        return View();
    }

    // GET: Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return View(users);
    }

    // GET: Admin/SetAdmin/{id}
    public async Task<IActionResult> SetAdmin(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Role = Role.Admin;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã cấp quyền Admin cho {user.FullName}";
        return RedirectToAction("Users");
    }

    // GET: Admin/RemoveAdmin/{id}
    public async Task<IActionResult> RemoveAdmin(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Role = Role.User;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã gỡ quyền Admin của {user.FullName}";
        return RedirectToAction("Users");
    }

    // GET: Admin/Profile
    public async Task<IActionResult> Profile()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: Admin/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(User model)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Update user info
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;

        await _context.SaveChangesAsync();

        // Update session
        HttpContext.Session.SetString("FullName", user.FullName);

        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Profile");
    }

    // GET: Admin/Users/Create
    public IActionResult CreateUser()
    {
        return View();
    }

    // POST: Admin/Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(User model, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Mật khẩu không được để trống.");
            return View(model);
        }

        // Check if username or email already exists
        if (await _context.Users.AnyAsync(u => u.Username == model.Username || u.Email == model.Email))
        {
            ModelState.AddModelError("", "Username hoặc Email đã tồn tại.");
            return View(model);
        }

        // Hash password
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var passwordHash = Convert.ToBase64String(hashedBytes);

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            Phone = model.Phone,
            FullName = model.FullName,
            PasswordHash = passwordHash,
            IsEmailVerified = true,
            Role = model.Role,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo tài khoản {user.FullName} thành công!";
        return RedirectToAction("Users");
    }

    // GET: Admin/Users/Edit/{id}
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: Admin/Users/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, User model, string newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if username or email already exists (excluding current user)
        if (await _context.Users.AnyAsync(u => (u.Username == model.Username || u.Email == model.Email) && u.Id != id))
        {
            ModelState.AddModelError("", "Username hoặc Email đã tồn tại.");
            return View(user);
        }

        // Update user info
        user.Username = model.Username;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.FullName = model.FullName;
        user.Role = model.Role;

        // Update password if provided
        if (!string.IsNullOrEmpty(newPassword))
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
            user.PasswordHash = Convert.ToBase64String(hashedBytes);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật thông tin {user.FullName} thành công!";
        return RedirectToAction("Users");
    }

    // POST: Admin/Users/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userName = user.FullName;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa tài khoản {userName} thành công!";
        return RedirectToAction("Users");
    }

    // ==================== PRODUCTS MANAGEMENT ====================

    // GET: Admin/Products
    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(products);
    }

    // GET: Admin/Products/Create
    public async Task<IActionResult> CreateProduct()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
        return View();
    }

    // POST: Admin/Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product model, IFormFile? mainImage, List<IFormFile>? images)
    {
        if (ModelState.IsValid)
        {
            // Save main image to file
            if (mainImage != null && mainImage.Length > 0)
            {
                model.MainImage = await SaveImageAsync(mainImage, "main");
            }

            // Save additional images to files
            if (images != null && images.Any(i => i.Length > 0))
            {
                var imagePaths = new List<string>();
                foreach (var image in images.Where(i => i.Length > 0))
                {
                    var imagePath = await SaveImageAsync(image, "additional");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imagePaths.Add(imagePath);
                    }
                }
                if (imagePaths.Any())
                {
                    model.Images = System.Text.Json.JsonSerializer.Serialize(imagePaths);
                }
            }

            // Handle StockByCapacity from form
            var stockByCapacityJson = Request.Form["StockByCapacity"].ToString();
            if (!string.IsNullOrEmpty(stockByCapacityJson))
            {
                model.StockByCapacity = stockByCapacityJson;
                // Calculate total stock from variants
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(stockByCapacityJson);
                    if (variants != null)
                    {
                        model.Stock = variants.Sum(v => v.ContainsKey("Stock") && v["Stock"] != null 
                            ? int.Parse(v["Stock"].ToString() ?? "0") 
                            : 0);
                    }
                }
                catch { }
            }

            model.CreatedAt = DateTime.Now;
            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo sản phẩm {model.Name} thành công!";
            return RedirectToAction("Products");
        }

        // Load categories again if ModelState is invalid
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
        return View(model);
    }

    // GET: Admin/Products/Edit/{id}
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Admin/Products/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product model, IFormFile? mainImage, List<IFormFile>? images)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Save new main image to file if provided
            if (mainImage != null && mainImage.Length > 0)
            {
                // Delete old main image if exists
                if (!string.IsNullOrEmpty(product.MainImage))
                {
                    DeleteImageFile(product.MainImage);
                }
                product.MainImage = await SaveImageAsync(mainImage, "main");
            }

            // Handle additional images
            if (images != null && images.Any(i => i.Length > 0))
            {
                var imagePaths = new List<string>();
                
                // Keep existing images (they are file paths now)
                if (!string.IsNullOrEmpty(product.Images))
                {
                    try
                    {
                        var existingPaths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.Images);
                        if (existingPaths != null)
                        {
                            imagePaths.AddRange(existingPaths);
                        }
                    }
                    catch { }
                }

                // Add new images as file paths
                foreach (var image in images.Where(i => i.Length > 0))
                {
                    var imagePath = await SaveImageAsync(image, "additional");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imagePaths.Add(imagePath);
                    }
                }

                product.Images = System.Text.Json.JsonSerializer.Serialize(imagePaths);
            }

            // Update other fields
            product.Name = model.Name;
            product.CategoryId = model.CategoryId;
            product.Description = model.Description;
            product.DetailedDescription = model.DetailedDescription;
            product.Price = model.Price;
            product.OriginalPrice = model.OriginalPrice;
            product.SKU = model.SKU;
            product.Tags = model.Tags;
            product.Brand = model.Brand;
            product.Origin = model.Origin;
            product.ExpiryDate = model.ExpiryDate;
            
            // Handle StockByCapacity from form
            var stockByCapacityJson = Request.Form["StockByCapacity"].ToString();
            if (!string.IsNullOrEmpty(stockByCapacityJson))
            {
                product.StockByCapacity = stockByCapacityJson;
                // Calculate total stock from variants
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(stockByCapacityJson);
                    if (variants != null)
                    {
                        product.Stock = variants.Sum(v => v.ContainsKey("Stock") && v["Stock"] != null 
                            ? int.Parse(v["Stock"].ToString() ?? "0") 
                            : 0);
                    }
                }
                catch { }
            }
            else
            {
                product.Stock = model.Stock;
            }
            product.Rating = model.Rating;
            product.ReviewCount = model.ReviewCount;
            product.IsActive = model.IsActive;
            product.IsFeatured = model.IsFeatured;
            product.IsNew = model.IsNew;
            product.OnSale = model.OnSale;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật sản phẩm {product.Name} thành công!";
            return RedirectToAction("Products");
        }

        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Admin/Products/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Delete image files
        if (!string.IsNullOrEmpty(product.MainImage))
        {
            DeleteImageFile(product.MainImage);
        }
        
        if (!string.IsNullOrEmpty(product.Images))
        {
            try
            {
                var imagePaths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.Images);
                if (imagePaths != null)
                {
                    foreach (var path in imagePaths)
                    {
                        DeleteImageFile(path);
                    }
                }
            }
            catch { }
        }

        var productName = product.Name;
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa sản phẩm {productName} thành công!";
        return RedirectToAction("Products");
    }

    // Helper methods for image handling
    private async Task<string> SaveImageAsync(IFormFile imageFile, string prefix)
    {
        if (imageFile == null || imageFile.Length == 0)
            return string.Empty;

        // Create uploads/products directory if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate unique filename
        var extension = Path.GetExtension(imageFile.FileName);
        var fileName = $"{prefix}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(fileStream);
        }

        // Return relative path from wwwroot
        return $"/uploads/products/{fileName}";
    }

    private void DeleteImageFile(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return;

        try
        {
            // Handle both relative paths (/uploads/...) and absolute paths
            string fullPath;
            if (imagePath.StartsWith("/"))
            {
                // Relative path from wwwroot
                fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }
            else if (imagePath.StartsWith("data:"))
            {
                // Old base64 format - skip deletion
                return;
            }
            else
            {
                // Assume it's already a full path
                fullPath = imagePath;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch
        {
            // Silently fail if file doesn't exist or can't be deleted
        }
    }

    // ==================== CATEGORIES MANAGEMENT ====================

    // GET: Admin/Categories
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(categories);
    }

    // GET: Admin/Categories/Create
    public IActionResult CreateCategory()
    {
        return View();
    }

    // POST: Admin/Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(Category model)
    {
        if (ModelState.IsValid)
        {
            model.CreatedAt = DateTime.Now;
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo danh mục {model.Name} thành công!";
            return RedirectToAction("Categories");
        }

        return View(model);
    }

    // GET: Admin/Categories/Edit/{id}
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST: Admin/Categories/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, Category model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            category.Name = model.Name;
            category.Description = model.Description;
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật danh mục {category.Name} thành công!";
            return RedirectToAction("Categories");
        }

        return View(model);
    }

    // POST: Admin/Categories/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Check if category has products
        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            TempData["Error"] = $"Không thể xóa danh mục {category.Name} vì còn sản phẩm đang sử dụng danh mục này!";
            return RedirectToAction("Categories");
        }

        var categoryName = category.Name;
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa danh mục {categoryName} thành công!";
        return RedirectToAction("Categories");
    }

    // GET: Admin/Orders
    public async Task<IActionResult> Orders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    // GET: Admin/Orders/Details/{id}
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: Admin/Orders/UpdateStatus/{id}
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        // Validate status transition
        var validTransition = IsValidStatusTransition(order.Status, status);
        if (!validTransition.isValid)
        {
            return Json(new { success = false, message = validTransition.message });
        }

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Json(new { 
            success = true, 
            message = $"Đã cập nhật trạng thái đơn hàng {order.OrderCode} từ \"{GetStatusName(oldStatus)}\" thành \"{GetStatusName(status)}\"",
            newStatus = status,
            newStatusName = GetStatusName(status),
            newStatusClass = GetStatusClass(status)
        });
    }

    // Validate status transition logic
    private (bool isValid, string message) IsValidStatusTransition(string currentStatus, string newStatus)
    {
        // Same status - no change needed
        if (currentStatus == newStatus)
        {
            return (false, "Trạng thái không thay đổi");
        }

        // Completed orders cannot be changed
        if (currentStatus == "completed")
        {
            return (false, "Đơn hàng đã hoàn thành không thể thay đổi trạng thái");
        }

        // Cancelled orders cannot be changed
        if (currentStatus == "cancelled")
        {
            return (false, "Đơn hàng đã hủy không thể thay đổi trạng thái");
        }

        // Allow cancellation from any active status
        if (newStatus == "cancelled")
        {
            return (true, "");
        }

        // Define valid transitions
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "pending", new List<string> { "confirmed", "cancelled" } },
            { "confirmed", new List<string> { "shipping", "cancelled" } },
            { "shipping", new List<string> { "completed", "cancelled" } }
        };

        if (validTransitions.TryGetValue(currentStatus, out var allowedStatuses))
        {
            if (allowedStatuses.Contains(newStatus))
            {
                return (true, "");
            }
            else
            {
                var currentName = GetStatusName(currentStatus);
                var newName = GetStatusName(newStatus);
                return (false, $"Không thể chuyển từ \"{currentName}\" sang \"{newName}\". Vui lòng thực hiện theo đúng quy trình!");
            }
        }

        return (false, "Trạng thái không hợp lệ");
    }

    private string GetStatusClass(string status)
    {
        return status switch
        {
            "pending" => "bg-warning",
            "confirmed" => "bg-info",
            "shipping" => "bg-primary",
            "completed" => "bg-success",
            "cancelled" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    private string GetStatusName(string status)
    {
        return status switch
        {
            "pending" => "Chờ xác nhận",
            "confirmed" => "Đã xác nhận",
            "shipping" => "Đang giao hàng",
            "completed" => "Hoàn thành",
            "cancelled" => "Đã hủy",
            _ => status
        };
    }

    // ==================== REPORTS & STATISTICS ====================

    // GET: Admin/Reports
    public async Task<IActionResult> Reports(DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        var thisYear = new DateTime(now.Year, 1, 1);
        var last7Days = today.AddDays(-7);
        var last30Days = today.AddDays(-30);

        // Date filter
        DateTime? filterStartDate = null;
        DateTime? filterEndDate = null;
        bool hasDateFilter = false;

        if (fromDate.HasValue || toDate.HasValue)
        {
            hasDateFilter = true;
            filterStartDate = fromDate?.Date ?? DateTime.MinValue;
            filterEndDate = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
            
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.FromDateDisplay = fromDate?.ToString("dd/MM/yyyy");
            ViewBag.ToDateDisplay = toDate?.ToString("dd/MM/yyyy");
        }
        else
        {
            ViewBag.FromDate = "";
            ViewBag.ToDate = "";
            ViewBag.FromDateDisplay = "";
            ViewBag.ToDateDisplay = "";
        }
        ViewBag.FilterActive = hasDateFilter;

        // Revenue Statistics
        var totalRevenueQuery = _context.Orders.Where(o => o.Status == "completed");
        if (hasDateFilter)
        {
            totalRevenueQuery = totalRevenueQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var totalRevenue = await totalRevenueQuery.SumAsync(o => (decimal?)o.Total) ?? 0;

        var todayRevenueQuery = _context.Orders.Where(o => o.Status == "completed" && o.CreatedAt >= today);
        if (hasDateFilter)
        {
            todayRevenueQuery = todayRevenueQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var todayRevenue = await todayRevenueQuery.SumAsync(o => (decimal?)o.Total) ?? 0;

        var thisMonthRevenueQuery = _context.Orders.Where(o => o.Status == "completed" && o.CreatedAt >= thisMonth);
        if (hasDateFilter)
        {
            thisMonthRevenueQuery = thisMonthRevenueQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var thisMonthRevenue = await thisMonthRevenueQuery.SumAsync(o => (decimal?)o.Total) ?? 0;

        var lastMonthRevenueQuery = _context.Orders.Where(o => o.Status == "completed" && o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth);
        if (hasDateFilter)
        {
            lastMonthRevenueQuery = lastMonthRevenueQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var lastMonthRevenue = await lastMonthRevenueQuery.SumAsync(o => (decimal?)o.Total) ?? 0;

        // Revenue by last 12 months for chart (or by filter range if filtered)
        var monthlyRevenue = new List<decimal>();
        var monthlyLabels = new List<string>();
        
        if (hasDateFilter)
        {
            // If filtered, show daily data within the range
            var currentDate = filterStartDate.Value;
            var endDate = filterEndDate.Value;
            var daysDiff = (endDate - currentDate).Days;
            
            // Limit to maximum 90 days for performance
            if (daysDiff > 90)
            {
                // Group by week if more than 90 days
                while (currentDate <= endDate)
                {
                    var weekEnd = currentDate.AddDays(6);
                    if (weekEnd > endDate) weekEnd = endDate;
                    
                    var weekRevenue = await _context.Orders
                        .Where(o => o.Status == "completed" && o.CreatedAt >= currentDate && o.CreatedAt <= weekEnd)
                        .SumAsync(o => (decimal?)o.Total) ?? 0;
                    monthlyRevenue.Add(weekRevenue);
                    monthlyLabels.Add(currentDate.ToString("dd/MM") + " - " + weekEnd.ToString("dd/MM"));
                    
                    currentDate = weekEnd.AddDays(1);
                }
            }
            else
            {
                // Daily data
                while (currentDate <= endDate)
                {
                    var dayEnd = currentDate.AddDays(1).AddTicks(-1);
                    var dayRevenue = await _context.Orders
                        .Where(o => o.Status == "completed" && o.CreatedAt >= currentDate && o.CreatedAt <= dayEnd)
                        .SumAsync(o => (decimal?)o.Total) ?? 0;
                    monthlyRevenue.Add(dayRevenue);
                    monthlyLabels.Add(currentDate.ToString("dd/MM"));
                    
                    currentDate = currentDate.AddDays(1);
                }
            }
        }
        else
        {
            // Default: last 12 months
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var monthRevenue = await _context.Orders
                    .Where(o => o.Status == "completed" && o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;
                monthlyRevenue.Add(monthRevenue);
                monthlyLabels.Add(monthStart.ToString("MM/yyyy"));
            }
        }

        // Order Statistics
        var allOrdersQuery = _context.Orders.AsQueryable();
        if (hasDateFilter)
        {
            allOrdersQuery = allOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var totalOrders = await allOrdersQuery.CountAsync();
        
        var completedOrdersQuery = _context.Orders.Where(o => o.Status == "completed");
        if (hasDateFilter)
        {
            completedOrdersQuery = completedOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var completedOrders = await completedOrdersQuery.CountAsync();
        
        var confirmedOrdersQuery = _context.Orders.Where(o => o.Status == "confirmed");
        if (hasDateFilter)
        {
            confirmedOrdersQuery = confirmedOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var confirmedOrders = await confirmedOrdersQuery.CountAsync();
        
        var pendingOrdersQuery = _context.Orders.Where(o => o.Status == "pending");
        if (hasDateFilter)
        {
            pendingOrdersQuery = pendingOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var pendingOrders = await pendingOrdersQuery.CountAsync();
        
        var shippingOrdersQuery = _context.Orders.Where(o => o.Status == "shipping");
        if (hasDateFilter)
        {
            shippingOrdersQuery = shippingOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var shippingOrders = await shippingOrdersQuery.CountAsync();
        
        var cancelledOrdersQuery = _context.Orders.Where(o => o.Status == "cancelled");
        if (hasDateFilter)
        {
            cancelledOrdersQuery = cancelledOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var cancelledOrders = await cancelledOrdersQuery.CountAsync();
        
        var todayOrdersQuery = _context.Orders.Where(o => o.CreatedAt >= today);
        if (hasDateFilter)
        {
            todayOrdersQuery = todayOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var todayOrders = await todayOrdersQuery.CountAsync();
        
        var thisMonthOrdersQuery = _context.Orders.Where(o => o.CreatedAt >= thisMonth);
        if (hasDateFilter)
        {
            thisMonthOrdersQuery = thisMonthOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var thisMonthOrders = await thisMonthOrdersQuery.CountAsync();

        // Orders by status for chart
        var ordersByStatus = new Dictionary<string, int>
        {
            { "Hoàn thành", completedOrders },
            { "Đang giao hàng", shippingOrders },
            { "Đã xác nhận", confirmedOrders },
            { "Chờ xác nhận", pendingOrders },
            { "Đã hủy", cancelledOrders }
        };

        // Orders by last 30 days for chart (or by filter range if filtered)
        var dailyOrders = new List<int>();
        var dailyLabels = new List<string>();
        
        if (hasDateFilter)
        {
            var currentDate = filterStartDate.Value;
            var endDate = filterEndDate.Value;
            var daysDiff = (endDate - currentDate).Days;
            
            // Limit to maximum 90 days for performance
            if (daysDiff > 90)
            {
                // Group by week if more than 90 days
                while (currentDate <= endDate)
                {
                    var weekEnd = currentDate.AddDays(6);
                    if (weekEnd > endDate) weekEnd = endDate;
                    
                    var weekOrderCount = await _context.Orders
                        .CountAsync(o => o.CreatedAt >= currentDate && o.CreatedAt <= weekEnd);
                    dailyOrders.Add(weekOrderCount);
                    dailyLabels.Add(currentDate.ToString("dd/MM") + " - " + weekEnd.ToString("dd/MM"));
                    
                    currentDate = weekEnd.AddDays(1);
                }
            }
            else
            {
                // Daily data
                while (currentDate <= endDate)
                {
                    var dayEnd = currentDate.AddDays(1).AddTicks(-1);
                    var dayOrderCount = await _context.Orders
                        .CountAsync(o => o.CreatedAt >= currentDate && o.CreatedAt <= dayEnd);
                    dailyOrders.Add(dayOrderCount);
                    dailyLabels.Add(currentDate.ToString("dd/MM"));
                    
                    currentDate = currentDate.AddDays(1);
                }
            }
        }
        else
        {
            // Default: last 30 days
            for (int i = 29; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var dayStart = day;
                var dayEnd = day.AddDays(1);
                var dayOrderCount = await _context.Orders
                    .CountAsync(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd);
                dailyOrders.Add(dayOrderCount);
                dailyLabels.Add(day.ToString("dd/MM"));
            }
        }

        // Customer Statistics
        var totalCustomersQuery = _context.Users.AsQueryable();
        if (hasDateFilter)
        {
            totalCustomersQuery = totalCustomersQuery.Where(u => u.CreatedAt >= filterStartDate && u.CreatedAt <= filterEndDate);
        }
        var totalCustomers = await totalCustomersQuery.CountAsync();
        
        var newCustomersThisMonthQuery = _context.Users.Where(u => u.CreatedAt >= thisMonth);
        if (hasDateFilter)
        {
            newCustomersThisMonthQuery = newCustomersThisMonthQuery.Where(u => u.CreatedAt >= filterStartDate && u.CreatedAt <= filterEndDate);
        }
        var newCustomersThisMonth = await newCustomersThisMonthQuery.CountAsync();
        
        var newCustomersLastMonthQuery = _context.Users.Where(u => u.CreatedAt >= lastMonth && u.CreatedAt < thisMonth);
        if (hasDateFilter)
        {
            newCustomersLastMonthQuery = newCustomersLastMonthQuery.Where(u => u.CreatedAt >= filterStartDate && u.CreatedAt <= filterEndDate);
        }
        var newCustomersLastMonth = await newCustomersLastMonthQuery.CountAsync();
        
        var customersWithOrdersQuery = _context.Orders.Where(o => o.UserId != null);
        if (hasDateFilter)
        {
            customersWithOrdersQuery = customersWithOrdersQuery.Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= filterEndDate);
        }
        var customersWithOrders = await customersWithOrdersQuery
            .Select(o => o.UserId)
            .Distinct()
            .CountAsync();

        // Customer registration by last 12 months for chart (or by filter range if filtered)
        var monthlyCustomers = new List<int>();
        var monthlyCustomerLabels = new List<string>();
        
        if (hasDateFilter)
        {
            var currentDate = filterStartDate.Value;
            var endDate = filterEndDate.Value;
            var daysDiff = (endDate - currentDate).Days;
            
            // Limit to maximum 90 days for performance
            if (daysDiff > 90)
            {
                // Group by week if more than 90 days
                while (currentDate <= endDate)
                {
                    var weekEnd = currentDate.AddDays(6);
                    if (weekEnd > endDate) weekEnd = endDate;
                    
                    var weekCustomerCount = await _context.Users
                        .CountAsync(u => u.CreatedAt >= currentDate && u.CreatedAt <= weekEnd);
                    monthlyCustomers.Add(weekCustomerCount);
                    monthlyCustomerLabels.Add(currentDate.ToString("dd/MM") + " - " + weekEnd.ToString("dd/MM"));
                    
                    currentDate = weekEnd.AddDays(1);
                }
            }
            else
            {
                // Daily data
                while (currentDate <= endDate)
                {
                    var dayEnd = currentDate.AddDays(1).AddTicks(-1);
                    var dayCustomerCount = await _context.Users
                        .CountAsync(u => u.CreatedAt >= currentDate && u.CreatedAt <= dayEnd);
                    monthlyCustomers.Add(dayCustomerCount);
                    monthlyCustomerLabels.Add(currentDate.ToString("dd/MM"));
                    
                    currentDate = currentDate.AddDays(1);
                }
            }
        }
        else
        {
            // Default: last 12 months
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var monthCustomerCount = await _context.Users
                    .CountAsync(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd);
                monthlyCustomers.Add(monthCustomerCount);
                monthlyCustomerLabels.Add(monthStart.ToString("MM/yyyy"));
            }
        }

        // Top products by revenue
        var topProductsQuery = _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.Status == "completed");
        
        if (hasDateFilter)
        {
            topProductsQuery = topProductsQuery.Where(oi => oi.Order.CreatedAt >= filterStartDate && oi.Order.CreatedAt <= filterEndDate);
        }
        
        var topProductsData = await topProductsQuery
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
            .Select(g => new
            {
                ProductName = g.Key.Name,
                Revenue = g.Sum(oi => oi.Price * oi.Quantity),
                Quantity = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();
        
        var topProducts = topProductsData.Select(x =>
        {
            var dict = new Dictionary<string, object>();
            dict["ProductName"] = x.ProductName;
            dict["Revenue"] = (decimal)x.Revenue;
            dict["Quantity"] = (int)x.Quantity;
            return dict;
        }).ToList();

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TodayRevenue = todayRevenue;
        ViewBag.ThisMonthRevenue = thisMonthRevenue;
        ViewBag.LastMonthRevenue = lastMonthRevenue;
        ViewBag.MonthlyRevenue = monthlyRevenue;
        ViewBag.MonthlyLabels = monthlyLabels;

        ViewBag.TotalOrders = totalOrders;
        ViewBag.CompletedOrders = completedOrders;
        ViewBag.ConfirmedOrders = confirmedOrders;
        ViewBag.PendingOrders = pendingOrders;
        ViewBag.ShippingOrders = shippingOrders;
        ViewBag.CancelledOrders = cancelledOrders;
        ViewBag.TodayOrders = todayOrders;
        ViewBag.ThisMonthOrders = thisMonthOrders;
        ViewBag.OrdersByStatus = ordersByStatus;
        ViewBag.DailyOrders = dailyOrders;
        ViewBag.DailyLabels = dailyLabels;

        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.NewCustomersThisMonth = newCustomersThisMonth;
        ViewBag.NewCustomersLastMonth = newCustomersLastMonth;
        ViewBag.CustomersWithOrders = customersWithOrders;
        ViewBag.MonthlyCustomers = monthlyCustomers;
        ViewBag.MonthlyCustomerLabels = monthlyCustomerLabels;

        ViewBag.TopProducts = topProducts;

        return View();
    }
}
