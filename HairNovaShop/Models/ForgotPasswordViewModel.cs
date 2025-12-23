using System.ComponentModel.DataAnnotations;

namespace HairNovaShop.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email hoặc số điện thoại là bắt buộc")]
    [Display(Name = "Email hoặc Số điện thoại")]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Display(Name = "Mã OTP")]
    public string? OTPCode { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string? ConfirmNewPassword { get; set; }
}
