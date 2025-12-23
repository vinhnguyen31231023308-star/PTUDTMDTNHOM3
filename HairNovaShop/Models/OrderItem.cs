using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    // Navigation property
    public Order? Order { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Navigation property
    public Product? Product { get; set; }

    [Required]
    [StringLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Capacity { get; set; } // Dung tích (nếu có)

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; } // Giá tại thời điểm đặt hàng

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; set; } // Price * Quantity
}
