using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class GioHang
{
    public long Id { get; set; }

    public Guid TaiKhoanId { get; set; }

    public DateTime NgayTao { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}
