using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class OTP
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public OTPType Type { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;
}

public enum OTPType
{
    Registration = 1,
    ForgotPassword = 2
}
