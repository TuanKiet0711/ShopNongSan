using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class TaiKhoanController : Controller
    {
        private readonly NongSanContext _db;
        public TaiKhoanController(NongSanContext db) => _db = db;

        // GET: /Customer/TaiKhoan/DangNhap
        [HttpGet, AllowAnonymous]
        public IActionResult DangNhap(string? returnUrl = null)
            => View(new DangNhapVM { ReturnUrl = returnUrl });

        // POST: /Customer/TaiKhoan/DangNhap
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapVM model)
        {
            if (!ModelState.IsValid) return View(model);

            // So khớp PLAINTEXT
            var user = await _db.TaiKhoans
                .FirstOrDefaultAsync(x => x.TenDangNhap == model.TenDangNhap && x.MatKhau == model.MatKhau);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Role, user.VaiTro)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = model.GhiNho });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            if (user.VaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase)
             || user.VaiTro.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            return RedirectToAction("Index", "SanPhams", new { area = "Customer" });
        }

        // GET: /Customer/TaiKhoan/DangKy
        [HttpGet, AllowAnonymous]
        public IActionResult DangKy() => View(new DangKyVM());

        // POST: /Customer/TaiKhoan/DangKy
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DangKyVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var ten = (model.TenDangNhap ?? "").Trim();
            bool existed = await _db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten);
            if (existed)
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // LƯU PLAINTEXT
            var tk = new TaiKhoan
            {
                Id = Guid.NewGuid(),
                TenDangNhap = ten,
                MatKhau = model.MatKhau,
                HoTen = (model.HoTen ?? "").Trim(),
                VaiTro = "Customer",
                NgayTao = DateTime.Now
            };

            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            // Đăng nhập luôn
            return await DangNhap(new DangNhapVM
            {
                TenDangNhap = ten,
                MatKhau = model.MatKhau,
                GhiNho = true
            });
        }

        // POST: /Customer/TaiKhoan/DangXuat
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "SanPhams", new { area = "Customer" });
        }

        // GET: /Customer/TaiKhoan/KhongCoQuyen
        [HttpGet, AllowAnonymous]
        public IActionResult KhongCoQuyen() => View();
    }
}
