using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DonHangChiTiet
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public int SanPhamId { get; set; }

    public decimal DonGia { get; set; }

    public int SoLuong { get; set; }

    public DateTime? NgayGiao { get; set; }

    public DateTime? NgayDat { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
