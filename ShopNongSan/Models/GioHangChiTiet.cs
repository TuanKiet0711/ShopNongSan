using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class GioHangChiTiet
{
    public long Id { get; set; }

    public long GioHangId { get; set; }

    public int SanPhamId { get; set; }

    public decimal DonGia { get; set; }

    public int SoLuong { get; set; }

    public virtual GioHang GioHang { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
