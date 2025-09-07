using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _db;
        public DonHangsController(NongSanContext db) => _db = db;

        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static string NewOrderCode() =>
            $"DH{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        // ==== BUY NOW: chỉ chuyển hướng sang Checkout (KHÔNG tạo đơn ở đây) ====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult BuyNow(int id, int qty = 1, string? returnUrl = null)
        {
            if (qty < 1) qty = 1;

            var checkoutUrl = Url.Action("Checkout", "DonHangs", new { area = "Customer", id, qty });
            // Chưa đăng nhập hoặc không phải Customer -> sang login rồi quay lại đúng Checkout
            if (!User.Identity?.IsAuthenticated ?? true || !User.IsInRole("Customer"))
                return RedirectToAction("DangNhap", "TaiKhoan", new { area = "Customer", returnUrl = checkoutUrl });

            return Redirect(checkoutUrl!);
        }

        // GET: /Customer/DonHangs/Checkout
        // - Nếu có id & qty => MUA NGAY (hiển thị duy nhất sản phẩm đó)
        // - Nếu không => lấy từ giỏ
        [HttpGet]
        public async Task<IActionResult> Checkout(int? id = null, int qty = 1)
        {
            // Prefill thông tin nhận hàng
            var tk = await _db.TaiKhoans.FirstAsync(x => x.Id == UserId);
            var tt = await _db.ThongTinNguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);

            var vm = new CheckoutVM
            {
                HoTen = tk.HoTen,
                SoDienThoai = tt?.SoDienThoai ?? "",
                DiaChi = tt?.DiaChi ?? "",
                GhiChu = tt?.GhiChu ?? ""               // ★ NEW: prefill ghi chú từ hồ sơ
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

        // POST: /Customer/DonHangs/Place → tạo đơn (mua ngay HOẶC từ giỏ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(CheckoutVM model)
        {
            // (1) Validate form nhập liệu
            if (!ModelState.IsValid)
            {
                // bind Items để hiển thị lại (mua ngay hoặc giỏ)
                if (model.IsBuyNow && model.BuyNowSanPhamId.HasValue)
                {
                    var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.BuyNowSanPhamId.Value);
                    if (sp != null)
                    {
                        model.Items = new List<CheckoutItemVM> {
                            new CheckoutItemVM {
                                SanPhamId = sp.Id,
                                TenSanPham = sp.Ten,
                                DonGia = sp.Gia,
                                SoLuong = Math.Max(1, model.BuyNowSoLuong),
                                HinhAnh = sp.HinhAnh
                            }
                        };
                    }
                }
                else
                {
                    var cart0 = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
                    if (cart0 != null)
                    {
                        model.Items = await _db.GioHangChiTiets
                            .Where(x => x.GioHangId == cart0.Id)
                            .Include(x => x.SanPham)
                            .Select(i => new CheckoutItemVM
                            {
                                SanPhamId = i.SanPhamId,
                                TenSanPham = i.SanPham!.Ten,
                                DonGia = i.DonGia,
                                SoLuong = i.SoLuong,
                                HinhAnh = i.SanPham.HinhAnh
                            }).ToListAsync();
                    }
                }
                return View("Checkout", model);
            }

            // (2) Cập nhật địa chỉ/điện thoại/ghi chú mặc định vào hồ sơ
            var tt = await _db.ThongTinNguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == UserId);
            if (tt == null)
            {
                tt = new ThongTinNguoiDung { Id = Guid.NewGuid(), TaiKhoanId = UserId };
                _db.ThongTinNguoiDungs.Add(tt);
            }
            tt.DiaChi = model.DiaChi;
            tt.SoDienThoai = model.SoDienThoai;
            tt.GhiChu = model.GhiChu;                 // ★ NEW: lưu Ghi chú từ CheckoutVM vào hồ sơ
            tt.PhuongThucThanhToan = model.PhuongThucThanhToan;   // << NEW
            await _db.SaveChangesAsync();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // ==== (3) Lấy danh sách mặt hàng để tạo đơn ====
                List<(int SanPhamId, decimal DonGia, int SoLuong, SanPham? Sp)> lines;

                if (model.IsBuyNow && model.BuyNowSanPhamId.HasValue)
                {
                    // MUA NGAY: fetch lại SP & giá từ server (không tin input)
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
                    // TỪ GIỎ
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

                // (4) Kiểm tra tồn kho
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

                // (5) Tạo đơn
                var tong = lines.Sum(l => l.DonGia * l.SoLuong);

                // mã đơn tránh trùng
                string code = NewOrderCode();
                int tries = 0;
                while (tries++ < 3 && await _db.DonHangs.AnyAsync(d => d.MaDonHang == code))
                    code = NewOrderCode();

                var order = new DonHang
                {
                    MaDonHang = code,
                    TaiKhoanId = UserId,
                    TongTien = tong,
                    TrangThai = "Pending"
                };
                _db.DonHangs.Add(order);
                await _db.SaveChangesAsync(); // có Id

                foreach (var l in lines)
                {
                    _db.DonHangChiTiets.Add(new DonHangChiTiet
                    {
                        DonHangId = order.Id,
                        SanPhamId = l.SanPhamId,
                        DonGia = l.DonGia,   // snapshot
                        SoLuong = l.SoLuong
                    });
                    l.Sp!.SoLuongTon -= l.SoLuong; // trừ kho
                }
                await _db.SaveChangesAsync();

                // (6) Nếu từ giỏ thì xoá giỏ
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

                TempData["pttt"] = model.PhuongThucThanhToan;
                TempData["toast"] = model.PhuongThucThanhToan == "BANK"
                    ? "Đặt hàng thành công! Vui lòng chuyển khoản theo hướng dẫn."
                    : "Đặt hàng thành công! Vui lòng thanh toán khi nhận hàng.";
                TempData["toastType"] = "success";

                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["toast"] = "Có lỗi khi tạo đơn: " + ex.Message;
                TempData["toastType"] = "danger";

                // Quay lại đúng nơi người dùng đang đi
                if (model.IsBuyNow && model.BuyNowSanPhamId.HasValue)
                    return RedirectToAction(nameof(Checkout), new { id = model.BuyNowSanPhamId.Value, qty = Math.Max(1, model.BuyNowSoLuong) });
                return RedirectToAction(nameof(Checkout));
            }
        }

        // GET: /Customer/DonHangs
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.DonHangs
                .Where(d => d.TaiKhoanId == UserId)
                .OrderByDescending(d => d.Id)
                .ToListAsync();
            return View(list);
        }

        // GET: /Customer/DonHangs/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.TaiKhoanId == UserId);

            if (don == null) return NotFound();

            ViewBag.PhuongThucThanhToan = TempData["pttt"] as string; // chỉ có ngay sau Place
            return View(don);
        }

        // Hủy đơn khi Pending (hoàn trả kho)
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
