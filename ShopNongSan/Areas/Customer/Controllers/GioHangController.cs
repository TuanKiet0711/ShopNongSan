using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")] // Chỉ khách hàng mới thao tác giỏ
    public class GioHangController : Controller
    {
        private readonly NongSanContext _db;
        public GioHangController(NongSanContext db) => _db = db;

        private Guid? CurrentUserId
        {
            get
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(id, out var g) ? g : null;
            }
        }

        /// Lấy giỏ hiện tại (tạo mới nếu chưa có)
        private async Task<GioHang> GetOrCreateCartAsync(Guid userId)
        {
            // Nếu bạn cho phép 1 user có nhiều giỏ lịch sử, ta lấy giỏ mới nhất (ở đây đơn giản là bất kỳ giỏ)
            var cart = await _db.GioHangs
                .AsNoTracking()
                .Where(x => x.TaiKhoanId == userId)
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefaultAsync();

            if (cart != null) return cart;

            cart = new GioHang { TaiKhoanId = userId, NgayTao = DateTime.UtcNow };
            _db.GioHangs.Add(cart);
            await _db.SaveChangesAsync();
            return cart;
        }

        // GET: /Customer/GioHang
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uid = CurrentUserId;
            if (uid == null)
                return RedirectToAction("DangNhap", "TaiKhoan", new { area = "Customer", returnUrl = Url.Action("Index", "GioHang", new { area = "Customer" }) });

            var cart = await GetOrCreateCartAsync(uid.Value);

            var items = await _db.GioHangChiTiets
                .Where(x => x.GioHangId == cart.Id)
                .Include(x => x.SanPham)
                .Select(x => new GioHangItemVM
                {
                    GioHangChiTietId = x.Id,
                    SanPhamId = x.SanPhamId,
                    TenSanPham = x.SanPham.Ten,
                    HinhAnh = x.SanPham.HinhAnh,
                    DonGia = x.DonGia,
                    SoLuong = x.SoLuong
                })
                .ToListAsync();

            var vm = new GioHangVM { GioHangId = cart.Id, Items = items };
            return View(vm);
        }

        // POST: /Customer/GioHang/Add
        // ... giữ nguyên using/namespace/lớp

        // POST: /Customer/GioHang/Add  (SỬA: thêm toast)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Add(int id, int qty = 1, string? returnUrl = null)
        {
            if (qty < 1) qty = 1;

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var back = string.IsNullOrWhiteSpace(returnUrl)
                    ? Url.Action("Index", "GioHang", new { area = "Customer" })
                    : returnUrl;
                return RedirectToAction("DangNhap", "TaiKhoan", new { area = "Customer", returnUrl = back });
            }

            var uid = CurrentUserId!.Value;

            var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (sp == null)
            {
                TempData["msg"] = "Sản phẩm không tồn tại.";
                return Redirect(returnUrl ?? Url.Action("Index", "SanPhams", new { area = "Customer" })!);
            }

            var cart = await GetOrCreateCartAsync(uid);

            var line = await _db.GioHangChiTiets
                .FirstOrDefaultAsync(x => x.GioHangId == cart.Id && x.SanPhamId == sp.Id);

            if (line == null)
            {
                line = new GioHangChiTiet
                {
                    GioHangId = cart.Id,
                    SanPhamId = sp.Id,
                    DonGia = sp.Gia,
                    SoLuong = qty
                };
                _db.GioHangChiTiets.Add(line);
            }
            else
            {
                line.SoLuong += qty;
                // line.DonGia = sp.Gia; // nếu muốn luôn dùng giá mới
            }

            await _db.SaveChangesAsync();

            // Toast thành công
            TempData["toast"] = $"Đã thêm \"{sp.Ten}\" vào giỏ hàng.";
            TempData["toastType"] = "success";

            // Optional: thông báo dạng alert cũ (nếu bạn vẫn muốn)
            TempData["msg"] = "Đã thêm vào giỏ hàng.";

            return Redirect(returnUrl ?? Url.Action("Index", "SanPhams", new { area = "Customer" })!);
        }

        // POST: /Customer/GioHang/Increase (TĂNG 1)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increase(long id)
        {
            var uid = CurrentUserId;
            if (uid == null) return Unauthorized();

            var line = await _db.GioHangChiTiets
                .Include(x => x.GioHang)
                .FirstOrDefaultAsync(x => x.Id == id && x.GioHang.TaiKhoanId == uid.Value);

            if (line == null) return NotFound();

            line.SoLuong += 1;
            await _db.SaveChangesAsync();

            TempData["toast"] = "Đã tăng số lượng.";
            TempData["toastType"] = "info";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Customer/GioHang/Decrease (GIẢM 1; nếu về 0 thì xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrease(long id)
        {
            var uid = CurrentUserId;
            if (uid == null) return Unauthorized();

            var line = await _db.GioHangChiTiets
                .Include(x => x.GioHang)
                .FirstOrDefaultAsync(x => x.Id == id && x.GioHang.TaiKhoanId == uid.Value);

            if (line == null) return NotFound();

            if (line.SoLuong > 1)
            {
                line.SoLuong -= 1;
            }
            else
            {
                _db.GioHangChiTiets.Remove(line);
            }

            await _db.SaveChangesAsync();

            TempData["toast"] = (line.SoLuong > 0) ? "Đã giảm số lượng." : "Đã xóa sản phẩm khỏi giỏ.";
            TempData["toastType"] = (line.SoLuong > 0) ? "info" : "warning";
            return RedirectToAction(nameof(Index));
        }


        // POST: /Customer/GioHang/UpdateQty
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQty(long id, int qty)
        {
            if (qty < 1) qty = 1;

            var uid = CurrentUserId;
            if (uid == null) return Unauthorized();

            var line = await _db.GioHangChiTiets
                .Include(x => x.GioHang)
                .FirstOrDefaultAsync(x => x.Id == id && x.GioHang.TaiKhoanId == uid.Value);

            if (line == null) return NotFound();

            line.SoLuong = qty;
            await _db.SaveChangesAsync();

            TempData["msg"] = "Đã cập nhật số lượng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Customer/GioHang/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(long id)
        {
            var uid = CurrentUserId;
            if (uid == null) return Unauthorized();

            var line = await _db.GioHangChiTiets
                .Include(x => x.GioHang)
                .FirstOrDefaultAsync(x => x.Id == id && x.GioHang.TaiKhoanId == uid.Value);

            if (line == null) return NotFound();

            _db.GioHangChiTiets.Remove(line);
            await _db.SaveChangesAsync();

            TempData["msg"] = "Đã xóa sản phẩm khỏi giỏ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Customer/GioHang/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var uid = CurrentUserId;
            if (uid == null) return Unauthorized();

            var cart = await _db.GioHangs.FirstOrDefaultAsync(x => x.TaiKhoanId == uid.Value);
            if (cart != null)
            {
                _db.GioHangs.Remove(cart); // ON DELETE CASCADE sẽ xóa chi tiết
                await _db.SaveChangesAsync();
            }

            TempData["msg"] = "Đã làm trống giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
