using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class NhatKyDangNhap
{
    public int Id { get; set; }

    public Guid? TaiKhoanId { get; set; }

    public string? TenDangNhap { get; set; }

    public string? DiaChiIp { get; set; }

    public string? KhoaRateLimit { get; set; }

    public string Endpoint { get; set; } = null!;

    public string PhuongThuc { get; set; } = null!;

    public bool ThanhCong { get; set; }

    public bool BiGioiHan { get; set; }

    public string? ThongBao { get; set; }

    public DateTime ThoiGian { get; set; }
}
