using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models.ViewModels
{
    public class CheckoutItemVM
    {
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; } = "";
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
        public string? HinhAnh { get; set; }
    }

    public class CheckoutVM
    {
        [Required, StringLength(100)]
        public string HoTen { get; set; } = "";

        [Required, StringLength(20)]
        public string SoDienThoai { get; set; } = "";

        [Required, StringLength(200)]
        public string DiaChi { get; set; } = "";

        [StringLength(300)]
        public string? GhiChu { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày giao hàng mong muốn")]
        public DateTime? NgayGiao { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [RegularExpression("COD|STRIPE", ErrorMessage = "Phương thức không hợp lệ")]
        public string PhuongThucThanhToan { get; set; } = "COD";


        // ==== MUA NGAY ====
        public bool IsBuyNow { get; set; }
        public int? BuyNowSanPhamId { get; set; }
        public int BuyNowSoLuong { get; set; } = 1;

        public List<CheckoutItemVM> Items { get; set; } = new();
        public decimal TongTien => Items.Sum(x => x.ThanhTien);
    }
}
