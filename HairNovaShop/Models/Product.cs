using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    // Foreign key to Category
    public int? CategoryId { get; set; }

    // Navigation properties
    public Category? Category { get; set; }
    
    public virtual ICollection<ProductVariant>? Variants { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(5000)]
    public string? DetailedDescription { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? OriginalPrice { get; set; }

    // Đường dẫn file hình ảnh chính (lưu trong wwwroot/uploads/products/)
    public string? MainImage { get; set; }

    // JSON string để lưu mảng các đường dẫn hình ảnh phụ (file paths)
    public string? Images { get; set; }

    [StringLength(100)]
    public string? SKU { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Origin { get; set; }

    [StringLength(200)]
    public string? ExpiryDate { get; set; }

    public int Stock { get; set; } = 0; // Tổng tồn kho (tính từ variants nếu có)

    // JSON string để lưu thông tin variants: [{"Capacity":"250ml","Price":null,"Stock":100},{"Capacity":"500ml","Price":null,"Stock":50}]
    public string? StockByCapacity { get; set; }

    [Range(0, 5)]
    public double Rating { get; set; } = 0;

    public int ReviewCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; } = false; // Sản phẩm nổi bật

    public bool IsNew { get; set; } = false; // Sản phẩm mới

    public bool OnSale { get; set; } = false; // Đang giảm giá

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
