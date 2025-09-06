using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ShopNongSan.Models
{
    // ===== DanhMuc =====
    public class DanhMucMetadata
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        public string Ten { get; set; } = null!;
    }

    [ModelMetadataType(typeof(DanhMucMetadata))]
    public partial class DanhMuc { }

    // ===== ThuongHieu =====
    public class ThuongHieuMetadata
    {
        [Required(ErrorMessage = "Tên thương hiệu không được để trống")]
        [Display(Name = "Tên thương hiệu")]
        public string Ten { get; set; } = null!;
    }

    [ModelMetadataType(typeof(ThuongHieuMetadata))]
    public partial class ThuongHieu { }

    // ===== SanPham =====
    public class SanPhamMetadata
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [Display(Name = "Tên sản phẩm")]
        public string Ten { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int DanhMucId { get; set; }

        // ThuongHieuId là tùy chọn -> để nullable, không đánh Required

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không hợp lệ")]
        [Display(Name = "Số lượng tồn")]
        public int SoLuongTon { get; set; }

        // HinhAnh: tùy chọn
    }

    [ModelMetadataType(typeof(SanPhamMetadata))]
    public partial class SanPham { }

    // ===== TaiKhoan =====
    public class TaiKhoanMetadata
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = null!;
    }

    [ModelMetadataType(typeof(TaiKhoanMetadata))]
    public partial class TaiKhoan { }
}
