using System.ComponentModel.DataAnnotations;

public class DangNhapVM
{
    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";

    public bool GhiNho { get; set; } = false;
    public string? ReturnUrl { get; set; }
}

public class DangKyVM
{
    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string HoTen { get; set; } = "";

    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";

    [Display(Name = "Nhập lại mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [DataType(DataType.Password)]
    [Compare("MatKhau", ErrorMessage = "Mật khẩu nhập lại không khớp")]
    public string NhapLaiMatKhau { get; set; } = "";
}

