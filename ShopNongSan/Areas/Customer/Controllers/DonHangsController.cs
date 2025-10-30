using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using ShopNongSan.Services;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer,Admin")]
    // KHÔNG đặt [Route] ở cấp controller để tránh xung đột với absolute routes bên dưới
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _db;
        private readonly IVnPayService _vnp;

        public DonHangsController(NongSanContext db, IVnPayService vnp)
        {
            _db = db;
            _vnp = vnp;
        }

        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static string NewOrderCode() =>
            $"DH{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        // ====== ROUTES ABSOLUTE (tránh 404 do map) ======

        // GET: /Customer/DonHangs  (và /Customer/DonHangs/Index)
        [HttpGet("/Customer/DonHangs")]
        [HttpGet("/Customer/DonHangs/Index")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.DonHangs
                .Where(d => d.TaiKhoanId == UserId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return View(list);
        }

        // GET: /Customer/DonHangs/Details/{id}
        [HttpGet("/Customer/DonHangs/Details/{id:long}")]
        public async Task<IActionResult> Details(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .Include(d => d.TaiKhoan).ThenInclude(t => t.ThongTinNguoiDung)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (don == null)
            {
                TempData["toast"] = "Không tìm thấy đơn hàng.";
                TempData["toastType"] = "danger";
                return Redirect("/Customer/DonHangs");
            }

            // Nếu là Customer thường thì chặn xem đơn người khác (không trả 404 mơ hồ)
            if (!User.IsInRole("Admin") && don.TaiKhoanId != UserId)
            {
                TempData["toast"] = "Bạn không có quyền xem đơn này.";
                TempData["toastType"] = "warning";
                return Redirect("/Customer/DonHangs");
            }

            return View(don);
        }

        // GET: /Customer/DonHangs/Checkout
        [HttpGet("/Customer/DonHangs/Checkout")]
        public async Task<IActionResult> Checkout(int? id = null, int qty = 1)
        {
            var vm = new CheckoutVM { NgayGiao = DateTime.Today.AddDays(1) };

            // MUA NGAY
            if (id.HasValue)
            {
                if (qty < 1) qty = 1;
                var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (sp == null)
                {
                    TempData["toast"] = "Sản phẩm không tồn tại.";
                    TempData["toastType"] = "danger";
                    return RedirectToAction("Index", "SanPhams", new { area = "Customer" });
                }

                vm.IsBuyNow = true;
                vm.BuyNowSanPhamId = sp.Id;
                vm.BuyNowSoLuong = qty;
                vm.Items = new()
                {
                    new CheckoutItemVM {
                        SanPhamId = sp.Id, TenSanPham = sp.Ten, DonGia = sp.Gia,
                        SoLuong = qty, HinhAnh = sp.HinhAnh
                    }
                };
                return View(vm);
            }

            // LẤY TỪ GIỎ
            var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
            if (cart == null)
            {
                TempData["toast"] = "Giỏ hàng trống.";
                TempData["toastType"] = "warning";
                return RedirectToAction("Index", "GioHang", new { area = "Customer" });
            }

            var items = await _db.GioHangChiTiets
                .Where(x => x.GioHangId == cart.Id)
                .Include(x => x.SanPham)
                .ToListAsync();

            if (items.Count == 0)
            {
                TempData["toast"] = "Giỏ hàng trống.";
                TempData["toastType"] = "warning";
                return RedirectToAction("Index", "GioHang", new { area = "Customer" });
            }

            vm.Items = items.Select(i => new CheckoutItemVM
            {
                SanPhamId = i.SanPhamId,
                TenSanPham = i.SanPham!.Ten,
                DonGia = i.DonGia,
                SoLuong = i.SoLuong,
                HinhAnh = i.SanPham.HinhAnh
            }).ToList();

            return View(vm);
        }

        // POST: /Customer/DonHangs/BuyNow
        [HttpPost("/Customer/DonHangs/BuyNow")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult BuyNow(int id, int qty = 1, string? returnUrl = null)
        {
            if (qty < 1) qty = 1;
            var checkoutUrl = Url.Action("Checkout", "DonHangs", new { area = "Customer", id, qty });

            if (!User.Identity?.IsAuthenticated ?? true || !User.IsInRole("Customer"))
                return RedirectToAction("DangNhap", "TaiKhoan", new { area = "Customer", returnUrl = checkoutUrl });

            return Redirect(checkoutUrl!);
        }

        // POST: /Customer/DonHangs/Place
        [HttpPost("/Customer/DonHangs/Place")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(CheckoutVM model)
        {
            if (!ModelState.IsValid) return View("Checkout", model);

            // Upsert an toàn cho ThongTinNguoiDung
            var tt = await _db.ThongTinNguoiDungs.SingleOrDefaultAsync(x => x.TaiKhoanId == UserId);
            if (tt == null)
            {
                tt = new ThongTinNguoiDung { Id = Guid.NewGuid(), TaiKhoanId = UserId };
                _db.ThongTinNguoiDungs.Add(tt); // INSERT
            }
            tt.DiaChi = model.DiaChi;
            tt.SoDienThoai = model.SoDienThoai;
            tt.GhiChu = model.GhiChu;
            tt.PhuongThucThanhToan = model.PhuongThucThanhToan;
            await _db.SaveChangesAsync();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // (1) Dòng hàng
                List<(int SanPhamId, decimal DonGia, int SoLuong, SanPham? Sp)> lines;

                if (model.IsBuyNow && model.BuyNowSanPhamId.HasValue)
                {
                    var sp = await _db.SanPhams.FirstOrDefaultAsync(x => x.Id == model.BuyNowSanPhamId.Value);
                    if (sp == null)
                    {
                        TempData["toast"] = "Sản phẩm không tồn tại.";
                        TempData["toastType"] = "danger";
                        return RedirectToAction("Index", "SanPhams", new { area = "Customer" });
                    }
                    lines = new() { (sp.Id, sp.Gia, Math.Max(1, model.BuyNowSoLuong), sp) };
                }
                else
                {
                    var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
                    if (cart == null)
                    {
                        TempData["toast"] = "Giỏ hàng trống.";
                        TempData["toastType"] = "warning";
                        return RedirectToAction("Index", "GioHang", new { area = "Customer" });
                    }
                    var items = await _db.GioHangChiTiets
                        .Where(x => x.GioHangId == cart.Id)
                        .Include(x => x.SanPham)
                        .ToListAsync();
                    if (items.Count == 0)
                    {
                        TempData["toast"] = "Giỏ hàng trống.";
                        TempData["toastType"] = "warning";
                        return RedirectToAction("Index", "GioHang", new { area = "Customer" });
                    }
                    lines = items.Select(i => (i.SanPhamId, i.DonGia, i.SoLuong, i.SanPham)).ToList();
                }

                // (2) Tồn kho
                foreach (var l in lines)
                {
                    if (l.Sp == null)
                    {
                        TempData["toast"] = "Có sản phẩm không còn tồn tại.";
                        TempData["toastType"] = "danger";
                        return RedirectToAction("Index", "GioHang", new { area = "Customer" });
                    }
                    if (l.Sp.SoLuongTon < l.SoLuong)
                    {
                        TempData["toast"] = $"\"{l.Sp.Ten}\" không đủ tồn kho.";
                        TempData["toastType"] = "warning";
                        return RedirectToAction("Index", "GioHang", new { area = "Customer" });
                    }
                }

                // (3) Tạo đơn + snapshot
                var tong = lines.Sum(l => l.DonGia * l.SoLuong);
                string code = NewOrderCode();
                while (await _db.DonHangs.AnyAsync(d => d.MaDonHang == code))
                    code = NewOrderCode();

                var now = DateTime.Now;
                var order = new DonHang
                {
                    MaDonHang = code,
                    TaiKhoanId = UserId,
                    TongTien = tong,
                    TrangThai = "Chờ xử lý",
                    NgayDat = now,
                    NgayGiao = model.NgayGiao,
                    HoTen = model.HoTen,
                    SoDienThoai = model.SoDienThoai,
                    DiaChi = model.DiaChi,
                    GhiChu = model.GhiChu,
                    PhuongThucThanhToan = model.PhuongThucThanhToan
                };
                _db.DonHangs.Add(order);
                await _db.SaveChangesAsync();

                foreach (var l in lines)
                {
                    _db.DonHangChiTiets.Add(new DonHangChiTiet
                    {
                        DonHangId = order.Id,
                        SanPhamId = l.SanPhamId,
                        DonGia = l.DonGia,
                        SoLuong = l.SoLuong,
                        NgayDat = now
                    });
                }
                await _db.SaveChangesAsync();

                if (model.PhuongThucThanhToan == "COD")
                {
                    foreach (var l in lines) l.Sp!.SoLuongTon -= l.SoLuong;
                    await _db.SaveChangesAsync();

                    if (!model.IsBuyNow)
                    {
                        var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
                        if (cart != null) { _db.GioHangs.Remove(cart); await _db.SaveChangesAsync(); }
                    }

                    await tx.CommitAsync();
                    TempData["toast"] = "Đặt hàng thành công!";
                    TempData["toastType"] = "success";
                    return Redirect($"/Customer/DonHangs/Details/{order.Id}");
                }
                else
                {
                    await tx.CommitAsync(); // đơn ở "Chờ xử lý", chưa trừ kho
                    var payUrl = _vnp.CreatePaymentUrl(order.MaDonHang, order.TongTien,
                        orderInfo: $"Thanh toan don hang {order.MaDonHang}");
                    return Redirect(payUrl);
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["toast"] = "Có lỗi khi tạo đơn: " + ex.Message;
                TempData["toastType"] = "danger";
                return RedirectToAction(nameof(Checkout));
            }
        }

        // GET: /Customer/DonHangs/VnPayReturn
        [HttpGet("/Customer/DonHangs/VnPayReturn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            if (!_vnp.ValidateReturn(Request.Query, out var code, out var resp, out var amount))
            {
                TempData["toast"] = "Chữ ký không hợp lệ.";
                TempData["toastType"] = "danger";
                return Redirect("/Customer/DonHangs");
            }

            var order = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.MaDonHang == code);

            if (order == null)
            {
                TempData["toast"] = "Không tìm thấy đơn hàng.";
                TempData["toastType"] = "danger";
                return Redirect("/Customer/DonHangs");
            }

            if (resp == "00")
            {
                if (!string.Equals(order.TrangThai, "Đã xác nhận", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(order.TrangThai, "Hoàn tất", StringComparison.OrdinalIgnoreCase))
                {
                    using var tx = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        foreach (var ct in order.DonHangChiTiets)
                            if (ct.SanPham != null) ct.SanPham.SoLuongTon -= ct.SoLuong;

                        order.TrangThai = "Đã xác nhận";
                        await _db.SaveChangesAsync();

                        var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == order.TaiKhoanId);
                        if (cart != null) { _db.GioHangs.Remove(cart); await _db.SaveChangesAsync(); }

                        await tx.CommitAsync();
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        TempData["toast"] = "Lỗi cập nhật sau thanh toán.";
                        TempData["toastType"] = "danger";
                        return Redirect("/Customer/DonHangs");
                    }
                }

                TempData["toast"] = $"Thanh toán VNPAY thành công cho đơn {order.MaDonHang}.";
                TempData["toastType"] = "success";
            }
            else
            {
                if (!string.Equals(order.TrangThai, "Đã xác nhận", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(order.TrangThai, "Hoàn tất", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(order.TrangThai, "Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    order.TrangThai = "Chờ xử lý";
                    await _db.SaveChangesAsync();
                }
                TempData["toast"] = "Thanh toán không thành công hoặc đã hủy.";
                TempData["toastType"] = "warning";
            }

            // Dùng URL tuyệt đối để tránh lệ thuộc route convention
            return Redirect($"/Customer/DonHangs/Details/{order.Id}");
        }

        // POST: /Customer/DonHangs/Pay
        [HttpPost("/Customer/DonHangs/Pay")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(long id)
        {
            var order = await _db.DonHangs.FirstOrDefaultAsync(d => d.Id == id && d.TaiKhoanId == UserId);
            if (order == null)
            {
                TempData["toast"] = "Không tìm thấy đơn hoặc không thuộc về bạn.";
                TempData["toastType"] = "danger";
                return Redirect("/Customer/DonHangs");
            }

            if (!(string.Equals(order.TrangThai, "Chờ xử lý", StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(order.PhuongThucThanhToan, "VNPAY", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["toast"] = "Đơn không đủ điều kiện thanh toán lại.";
                TempData["toastType"] = "warning";
                return Redirect($"/Customer/DonHangs/Details/{id}");
            }

            var url = _vnp.CreatePaymentUrl(order.MaDonHang, order.TongTien, $"Thanh toan don hang {order.MaDonHang}");
            return Redirect(url);
        }

        // POST: /Customer/DonHangs/Cancel
        [HttpPost("/Customer/DonHangs/Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.TaiKhoanId == UserId);

            if (don == null)
            {
                TempData["toast"] = "Không tìm thấy đơn hoặc không thuộc về bạn.";
                TempData["toastType"] = "danger";
                return Redirect("/Customer/DonHangs");
            }

            if (!string.Equals(don.TrangThai, "Chờ xử lý", StringComparison.OrdinalIgnoreCase))
            {
                TempData["toast"] = "Chỉ hủy được khi đơn đang Chờ xử lý.";
                TempData["toastType"] = "warning";
                return Redirect($"/Customer/DonHangs/Details/{id}");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var ct in don.DonHangChiTiets)
                    if (ct.SanPham != null) ct.SanPham.SoLuongTon += ct.SoLuong;

                don.TrangThai = "Đã hủy";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["toast"] = $"Đã hủy đơn {don.MaDonHang}.";
                TempData["toastType"] = "success";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["toast"] = "Hủy đơn thất bại: " + ex.Message;
                TempData["toastType"] = "danger";
            }

            return Redirect($"/Customer/DonHangs/Details/{id}");
        }
    }
}
