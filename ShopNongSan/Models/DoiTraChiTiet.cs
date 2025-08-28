using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DoiTraChiTiet
{
    public long Id { get; set; }

    public long DoiTraId { get; set; }

    public long DonHangChiTietId { get; set; }

    public int SoLuong { get; set; }

    public virtual DoiTra DoiTra { get; set; } = null!;

    public virtual DonHangChiTiet DonHangChiTiet { get; set; } = null!;
}
