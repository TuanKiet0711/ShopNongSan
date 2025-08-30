using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models.ViewModels
{
    public class SanPhamFilterVM
    {
        [Display(Name = "Từ khóa")]
        public string? Q { get; set; }

        [Display(Name = "Danh mục")]
        public int? DanhMucId { get; set; }

        [Display(Name = "Thương hiệu")]
        public int? ThuongHieuId { get; set; }
        // NHẬN DẠNG CHUỖI để người dùng gõ: "1,500,000 đ"
        [Display(Name = "Giá từ")]
        public string? GiaMinStr { get; set; }

        [Display(Name = "đến")]
        public string? GiaMaxStr { get; set; }

        public string? Sort { get; set; } // "", price_asc, price_desc, newest
    }
}
