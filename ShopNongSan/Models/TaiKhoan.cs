using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class TaiKhoan
{
    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string VaiTro { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ThongTinNguoiDung? ThongTinNguoiDung { get; set; }
}
