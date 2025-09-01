using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class SanPham
{
    public int Id { get; set; }

    public string Ten { get; set; } = null!;

    public int DanhMucId { get; set; }

    public int? ThuongHieuId { get; set; }

    public decimal Gia { get; set; }

    public int SoLuongTon { get; set; }

    public string? HinhAnh { get; set; }

    public virtual DanhMuc DanhMuc { get; set; } = null!;

    public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; } = new List<DonHangChiTiet>();

    public virtual ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();

    public virtual ThuongHieu? ThuongHieu { get; set; }
}
