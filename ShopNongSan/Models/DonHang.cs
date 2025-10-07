using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DonHang
{
    public long Id { get; set; }

    public string MaDonHang { get; set; } = null!;

    public Guid TaiKhoanId { get; set; }

    public decimal TongTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? NgayGiao { get; set; }

    public DateTime? NgayDat { get; set; }

    public string? HoTen { get; set; }

    public string? SoDienThoai { get; set; }

    public string? DiaChi { get; set; }

    public string? GhiChu { get; set; }

    public string? PhuongThucThanhToan { get; set; }

    public virtual ICollection<DoiTra> DoiTras { get; set; } = new List<DoiTra>();

    public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; } = new List<DonHangChiTiet>();

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}
