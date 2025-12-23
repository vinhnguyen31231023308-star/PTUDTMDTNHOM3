using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class VerifyOTPViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số")]
    [Display(Name = "Mã OTP")]
    public string OTPCode { get; set; } = string.Empty;

    public OTPType Type { get; set; }

    // Cho quên mật khẩu
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string? ConfirmNewPassword { get; set; }

    // Cho đăng ký
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
