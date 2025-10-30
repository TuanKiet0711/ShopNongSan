using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Services;

namespace ShopNongSan.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly NongSanContext _db;
        private readonly IVnPayService _vnp;
        public PaymentController(NongSanContext db, IVnPayService vnp) { _db = db; _vnp = vnp; }

        [HttpPost("vnpay-ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            if (!_vnp.ValidateReturn(Request.Query, out var code, out var resp, out var amount))
                return Ok("INVALID|CHECKSUM");

            var order = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.MaDonHang == code);

            if (order == null) return Ok("INVALID|ORDER_NOT_FOUND");

            if (resp == "00")
            {
                if (!string.Equals(order.TrangThai, "Đã xác nhận", StringComparison.OrdinalIgnoreCase))
                {
                    using var tx = await _db.Database.BeginTransactionAsync();
                    foreach (var ct in order.DonHangChiTiets)
                        if (ct.SanPham != null) ct.SanPham.SoLuongTon -= ct.SoLuong;
                    order.TrangThai = "Đã xác nhận";
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                return Ok("OK");
            }

            order.TrangThai = "Chờ xử lý";
            await _db.SaveChangesAsync();
            return Ok("FAILED");
        }
    }
}
