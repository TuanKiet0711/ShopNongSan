using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer,Admin")] // Mặc định cần login
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

        /// <summary>Lấy giỏ hiện tại (tạo mới nếu chưa có)</summary>
        private async Task<GioHang> GetOrCreateCartAsync(Guid userId)
        {
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
                return RedirectToAction(
                    "DangNhap",
                    "TaiKhoan",
                    new { area = "Customer", returnUrl = Url.Action("Index", "GioHang", new { area = "Customer" }) }
                );

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

        // POST: /Customer/GioHang/Add (submit thường — có redirect)
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
            }

            await _db.SaveChangesAsync();

            TempData["toast"] = $"Đã thêm \"{sp.Ten}\" vào giỏ hàng.";
            TempData["toastType"] = "success";

            return Redirect(returnUrl ?? Url.Action("Index", "SanPhams", new { area = "Customer" })!);
        }

        // POST: /Customer/GioHang/AddAjax (AJAX — chưa login → 401)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // <— Cho phép vào action để TỰ trả 401 thay vì bị redirect 302
        public async Task<IActionResult> AddAjax(int id, int qty = 1)
        {
            if (qty < 1) qty = 1;

            // Chưa đăng nhập: trả 401 để JS bắt và redirect Login
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized(new { ok = false, message = "Bạn chưa đăng nhập." });

            var uid = CurrentUserId!.Value;

            var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (sp == null) return NotFound(new { ok = false, message = "Sản phẩm không tồn tại." });

            var cart = await GetOrCreateCartAsync(uid);

            var line = await _db.GioHangChiTiets
                .FirstOrDefaultAsync(x => x.GioHangId == cart.Id && x.SanPhamId == sp.Id);

            if (line == null)
            {
                _db.GioHangChiTiets.Add(new GioHangChiTiet
                {
                    GioHangId = cart.Id,
                    SanPhamId = sp.Id,
                    DonGia = sp.Gia,
                    SoLuong = qty
                });
            }
            else
            {
                line.SoLuong += qty;
            }

            await _db.SaveChangesAsync();

            // Tổng số lượng trong giỏ
            var cartCount = await _db.GioHangChiTiets
                .Where(x => x.GioHangId == cart.Id)
                .SumAsync(x => (int?)x.SoLuong) ?? 0;

            return Ok(new
            {
                ok = true,
                message = $"Đã thêm \"{sp.Ten}\" vào giỏ hàng.",
                cartCount
            });
        }

        // POST: /Customer/GioHang/Increase
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

            return RedirectToAction(nameof(Index));
        }

        // POST: /Customer/GioHang/Decrease
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

            return RedirectToAction(nameof(Index));
        }
    }
}
