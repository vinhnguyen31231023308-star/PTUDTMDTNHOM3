using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HairNovaShop.Models;

public class ProductVariant
{
    public int Id { get; set; }

    // Foreign key to Product
    public int ProductId { get; set; }

    // Navigation property
    public Product? Product { get; set; }

    [Required]
    [StringLength(50)]
    public string Capacity { get; set; } = string.Empty; // Ví dụ: "250ml", "500ml", "1L"

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; } // Giá riêng cho variant này (nếu null thì dùng giá của Product)

    [Range(0, int.MaxValue)]
    public int Stock { get; set; } = 0; // Số lượng tồn kho

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
