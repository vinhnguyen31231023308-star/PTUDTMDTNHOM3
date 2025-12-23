using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderCode { get; set; } = string.Empty; // Mã đơn hàng (VD: ORD20250115123456)

    public int? UserId { get; set; } // Nullable vì có thể đặt hàng không cần đăng nhập

    // Navigation property
    public User? User { get; set; }

    // Thông tin khách hàng
    [Required]
    [StringLength(255)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    // Địa chỉ giao hàng
    [Required]
    [StringLength(255)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ShippingProvince { get; set; }

    [StringLength(100)]
    public string? ShippingWard { get; set; }

    // Địa chỉ khác (nếu có)
    [StringLength(255)]
    public string? ShippingOtherAddress { get; set; }

    [StringLength(100)]
    public string? ShippingOtherProvince { get; set; }

    [StringLength(100)]
    public string? ShippingOtherWard { get; set; }

    [StringLength(20)]
    public string? ShippingOtherPhone { get; set; }

    // Tổng tiền
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; } = 0;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }

    // Phương thức thanh toán
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "cod"; // cod, bank, momo, vnpay

    // Trạng thái đơn hàng
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "pending"; // pending, confirmed, shipping, completed, cancelled

    // Ghi chú
    [StringLength(1000)]
    public string? Note { get; set; }

    // Voucher/Mã giảm giá
    [StringLength(50)]
    public string? VoucherCode { get; set; }

    // Ngày đặt hàng
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property cho OrderItems
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
