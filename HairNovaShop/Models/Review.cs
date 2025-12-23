using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Navigation property
    public Product? Product { get; set; }

    [Required]
    public int UserId { get; set; }

    // Navigation property
    public User? User { get; set; }

    [Required]
    public int OrderId { get; set; }

    // Navigation property
    public Order? Order { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; } // 1-5 stars

    [StringLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
