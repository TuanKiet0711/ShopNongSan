using System;
using System.Collections.Generic;

namespace ShopNongSan.Models;

public partial class DanhMuc
{
    public int Id { get; set; }

    public string Ten { get; set; } = null!;

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
