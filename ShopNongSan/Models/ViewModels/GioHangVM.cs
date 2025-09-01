using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models.ViewModels
{
    public class GioHangItemVM
    {
        public long GioHangChiTietId { get; set; }
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; } = "";
        public string? HinhAnh { get; set; }
        public decimal DonGia { get; set; }
        [Range(1, int.MaxValue)]
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    public class GioHangVM
    {
        public long GioHangId { get; set; }
        public List<GioHangItemVM> Items { get; set; } = new();
        public decimal TongTien => Items.Sum(x => x.ThanhTien);
    }
}
