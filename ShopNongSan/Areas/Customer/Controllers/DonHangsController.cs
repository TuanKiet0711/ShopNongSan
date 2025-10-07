using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer,Admin")]
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _db;
        public DonHangsController(NongSanContext db) => _db = db;

        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static string NewOrderCode() =>
            $"DH{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        // ==== BUY NOW ====
        [HttpPost]
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

        // GET: /Customer/DonHangs/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout(int? id = null, int qty = 1)
        {
            var tk = await _db.TaiKhoans.FirstAsync(x => x.Id == UserId);
            var tt = await _db.ThongTinNguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);

            var vm = new CheckoutVM
            {
                HoTen = tk.HoTen,
                SoDienThoai = tt?.SoDienThoai ?? "",
                DiaChi = tt?.DiaChi ?? "",
                GhiChu = tt?.GhiChu ?? "",
                NgayGiao = DateTime.Today.AddDays(1)   // mặc định ngày mai
            };

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
                vm.Items = new List<CheckoutItemVM>
                {
                    new CheckoutItemVM {
                        SanPhamId = sp.Id,
                        TenSanPham = sp.Ten,
                        DonGia = sp.Gia,
                        SoLuong = qty,
                        HinhAnh = sp.HinhAnh
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

        // POST: /Customer/DonHangs/Place
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(CheckoutVM model)
        {
            if (!ModelState.IsValid)
                return View("Checkout", model);

            // Cập nhật hồ sơ (giữ nguyên như bạn đã làm)
            var tt = await _db.ThongTinNguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
            if (tt == null)
            {
                tt = new ThongTinNguoiDung { Id = Guid.NewGuid(), TaiKhoanId = UserId };
                _db.ThongTinNguoiDungs.Add(tt);
            }
            tt.DiaChi = model.DiaChi;
            tt.SoDienThoai = model.SoDienThoai;
            tt.GhiChu = model.GhiChu;
            tt.PhuongThucThanhToan = model.PhuongThucThanhToan;
            await _db.SaveChangesAsync();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // (1) Chuẩn bị dòng hàng (giữ nguyên)
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
                    int q = Math.Max(1, model.BuyNowSoLuong);
                    lines = new() { (sp.Id, sp.Gia, q, sp) };
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

                // (2) Kiểm tra tồn kho (giữ nguyên)
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

                // (3) Tạo đơn + LƯU SNAPSHOT CHECKOUT
                var tong = lines.Sum(l => l.DonGia * l.SoLuong);
                string code = NewOrderCode();
                while (await _db.DonHangs.AnyAsync(d => d.MaDonHang == code))
                    code = NewOrderCode();

                var now = DateTime.Now; // hoặc DateTime.UtcNow tuỳ dự án
                var order = new DonHang
                {
                    MaDonHang = code,
                    TaiKhoanId = UserId,
                    TongTien = tong,
                    TrangThai = "Pending",
                    NgayDat = now,
                    NgayGiao = model.NgayGiao,

                    // SNAPSHOT từ CheckoutVM
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
                    l.Sp!.SoLuongTon -= l.SoLuong;
                }
                await _db.SaveChangesAsync();

                if (!model.IsBuyNow)
                {
                    var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
                    if (cart != null)
                    {
                        _db.GioHangs.Remove(cart);
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                TempData["toast"] = "Đặt hàng thành công!";
                TempData["toastType"] = "success";

                return RedirectToAction("Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["toast"] = "Có lỗi khi tạo đơn: " + ex.Message;
                TempData["toastType"] = "danger";
                return RedirectToAction(nameof(Checkout));
            }
        }

        // GET: /Customer/DonHangs
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.DonHangs
                .Where(d => d.TaiKhoanId == UserId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(list);
        }

        // GET: /Customer/DonHangs/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.TaiKhoanId == UserId);

            if (don == null) return NotFound();

            return View(don);
        }

        // POST: /Customer/DonHangs/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.TaiKhoanId == UserId);

            if (don == null) return NotFound();

            if (!string.Equals(don.TrangThai, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["toast"] = "Chỉ hủy được khi đơn đang Chờ xác nhận (Pending).";
                TempData["toastType"] = "warning";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var ct in don.DonHangChiTiets)
                    if (ct.SanPham != null)
                        ct.SanPham.SoLuongTon += ct.SoLuong;

                don.TrangThai = "Cancelled";
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

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
