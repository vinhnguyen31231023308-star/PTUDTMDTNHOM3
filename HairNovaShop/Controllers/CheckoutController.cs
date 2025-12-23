using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HairNovaShop.Data;
using HairNovaShop.Models;
using HairNovaShop.Services;
using Microsoft.AspNetCore.Http;

namespace HairNovaShop.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly ApplicationDbContext _context;

    public CheckoutController(ICartService cartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    // GET: Checkout/Index
    public IActionResult Index()
    {
        var cartItems = _cartService.GetCartItems(HttpContext.Session);
        
        if (cartItems == null || !cartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi đặt hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Load product details for each cart item
        var cartItemsWithProducts = new List<CartItem>();
        decimal subtotal = 0;
        
        foreach (var item in cartItems)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == item.ProductId);

            if (product != null)
            {
                var price = product.Price;
                
                // Nếu có capacity, tính giá từ variant
                if (!string.IsNullOrEmpty(item.Capacity) && !string.IsNullOrEmpty(product.StockByCapacity))
                {
                    try
                    {
                        var variants = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(product.StockByCapacity) ?? new List<Dictionary<string, object>>();
                        var variant = variants.FirstOrDefault(v => v?.GetValueOrDefault("Capacity")?.ToString() == item.Capacity);
                        if (variant != null && variant.ContainsKey("Price") && variant["Price"] != null)
                        {
                            var variantPrice = variant["Price"].ToString();
                            if (decimal.TryParse(variantPrice, out var vPrice) && vPrice > 0)
                            {
                                price = vPrice;
                            }
                        }
                    }
                    catch { }
                }
                
                cartItemsWithProducts.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.MainImage ?? "/images/placeholder.png",
                    Price = price,
                    Quantity = item.Quantity,
                    Capacity = item.Capacity
                });
                
                subtotal += price * item.Quantity;
            }
        }

        ViewBag.CartItems = cartItemsWithProducts;
        ViewBag.Subtotal = subtotal;
        ViewBag.Total = subtotal;

        // Nếu đã đăng nhập, lấy thông tin user
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                ViewBag.UserFullName = user.FullName;
                ViewBag.UserEmail = user.Email;
                ViewBag.UserPhone = user.Phone;
            }
        }

        return View();
    }

    // GET: Checkout/GetSavedAddresses
    [HttpGet]
    public async Task<IActionResult> GetSavedAddresses()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập" });
        }

        // Lấy các địa chỉ từ orders đã có
        var orders = await _context.Orders
            .Where(o => o.UserId == userId && !string.IsNullOrEmpty(o.ShippingAddress))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Group by unique addresses (in memory)
        var uniqueAddresses = orders
            .GroupBy(o => new { 
                ShippingAddress = o.ShippingAddress, 
                ShippingProvince = o.ShippingProvince ?? "", 
                ShippingWard = o.ShippingWard ?? "", 
                CustomerName = o.CustomerName, 
                CustomerPhone = o.CustomerPhone, 
                CustomerEmail = o.CustomerEmail 
            })
            .Select(g => g.First())
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new
            {
                customerName = o.CustomerName,
                customerPhone = o.CustomerPhone,
                customerEmail = o.CustomerEmail,
                shippingAddress = o.ShippingAddress,
                shippingProvince = o.ShippingProvince ?? "",
                shippingWard = o.ShippingWard ?? ""
            })
            .ToList();

        return Json(new { success = true, addresses = uniqueAddresses });
    }

    // POST: Checkout/PlaceOrder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string fullname, string phone, string email,
        string province, string ward, string address,
        string? voucherCode, string? paymentMethod,
        string? note, bool promoEmail = false,
        string? shipOther = null,
        string? shipName = null, string? shipProvince = null,
        string? shipWard = null, string? shipAddress = null,
        string? shipPhone = null,
        decimal discountAmount = 0)
    {
        var cartItems = _cartService.GetCartItems(HttpContext.Session);
        
        if (cartItems == null || !cartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        // Validation
        if (string.IsNullOrWhiteSpace(fullname) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(province) ||
            string.IsNullOrWhiteSpace(ward) || string.IsNullOrWhiteSpace(address))
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
            return RedirectToAction("Index");
        }

        // Generate order code: ORD + YYYYMMDD + 6 random digits
        var orderCode = GenerateOrderCode();

        // Calculate totals
        decimal subtotal = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in cartItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null) continue;

            var price = product.Price;
            
            // Nếu có capacity, tính giá từ variant
            if (!string.IsNullOrEmpty(item.Capacity) && !string.IsNullOrEmpty(product.StockByCapacity))
            {
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(product.StockByCapacity) ?? new List<Dictionary<string, object>>();
                    var variant = variants.FirstOrDefault(v => v?.GetValueOrDefault("Capacity")?.ToString() == item.Capacity);
                    if (variant != null && variant.ContainsKey("Price") && variant["Price"] != null)
                    {
                        var variantPrice = variant["Price"].ToString();
                        if (decimal.TryParse(variantPrice, out var vPrice) && vPrice > 0)
                        {
                            price = vPrice;
                        }
                    }
                }
                catch { }
            }

            var itemSubtotal = price * item.Quantity;
            subtotal += itemSubtotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Capacity = item.Capacity,
                Quantity = item.Quantity,
                Price = price,
                Subtotal = itemSubtotal
            });
        }

        var total = subtotal - discountAmount;
        if (total < 0) total = 0;

        // Get user ID if logged in
        int? userId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedUserId))
        {
            userId = parsedUserId;
        }

        // Create order
        var order = new Order
        {
            OrderCode = orderCode,
            UserId = userId,
            CustomerName = fullname,
            CustomerPhone = phone,
            CustomerEmail = email,
            ShippingAddress = address,
            ShippingProvince = province,
            ShippingWard = ward,
            ShippingOtherAddress = (shipOther == "true" || shipOther == "True") ? shipAddress : null,
            ShippingOtherProvince = (shipOther == "true" || shipOther == "True") ? shipProvince : null,
            ShippingOtherWard = (shipOther == "true" || shipOther == "True") ? shipWard : null,
            ShippingOtherPhone = (shipOther == "true" || shipOther == "True") ? shipPhone : null,
            Subtotal = subtotal,
            Discount = discountAmount,
            Total = total,
            PaymentMethod = paymentMethod ?? "cod",
            Status = "pending",
            Note = note,
            VoucherCode = voucherCode,
            CreatedAt = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // Save to get Order.Id

        // Add order items
        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
            _context.OrderItems.Add(item);
        }

        await _context.SaveChangesAsync();

        // Clear cart
        _cartService.ClearCart(HttpContext.Session);

        TempData["OrderSuccess"] = $"Đặt hàng thành công! Mã đơn hàng: {orderCode}";
        return RedirectToAction("Index", "Shop");
    }

    private string GenerateOrderCode()
    {
        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var random = new Random();
        var randomPart = random.Next(100000, 999999).ToString();
        return $"ORD{datePart}{randomPart}";
    }
}
