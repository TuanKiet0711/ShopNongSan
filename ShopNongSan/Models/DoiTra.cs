using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DoiTra
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public string? LyDo { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<DoiTraChiTiet> DoiTraChiTiets { get; set; } = new List<DoiTraChiTiet>();

    public virtual DonHang DonHang { get; set; } = null!;
}
