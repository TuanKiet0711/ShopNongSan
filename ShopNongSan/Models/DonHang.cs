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

    public virtual ICollection<DoiTra> DoiTras { get; set; } = new List<DoiTra>();

    public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; } = new List<DonHangChiTiet>();

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}
