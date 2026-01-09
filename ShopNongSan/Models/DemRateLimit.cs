using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DemRateLimit
{
    public int Id { get; set; }

    public string GiaTriKhoa { get; set; } = null!;

    public string Endpoint { get; set; } = null!;

    public DateTime BatDauCuaSo { get; set; }

    public DateTime KetThucCuaSo { get; set; }

    public int SoLuong { get; set; }

    public DateTime CapNhatLuc { get; set; }
}
