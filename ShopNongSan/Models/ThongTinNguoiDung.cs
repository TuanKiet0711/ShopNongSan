using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class ThongTinNguoiDung
{
    public Guid Id { get; set; }

    public Guid TaiKhoanId { get; set; }

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? GhiChu { get; set; }

    public string? PhuongThucThanhToan { get; set; }  // "COD" | "BANK"

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}
