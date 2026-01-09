using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Services;
using System.Security.Claims;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class TaiKhoanController : Controller
    {
        private readonly NongSanContext _db;
        private readonly RateLimitService _rate;

        public TaiKhoanController(NongSanContext db, RateLimitService rate)
        {
            _db = db;
            _rate = rate;
        }

        // GET: /Customer/TaiKhoan/DangNhap
        [HttpGet, AllowAnonymous]
        public IActionResult DangNhap(string? returnUrl = null)
            => View(new DangNhapVM { ReturnUrl = returnUrl });

        // POST: /Customer/TaiKhoan/DangNhap
       [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
public async Task<IActionResult> DangNhap(DangNhapVM model)
{
    if (!ModelState.IsValid) return View(model);

    string endpoint = "/tai-khoan/dang-nhap";
    string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    string key = RateLimitService.BuildKey(model.TenDangNhap, ip);

    // 1) Check đang bị chặn không
    var blocked = await _rate.IsBlockedAsync(key, endpoint);
    model.FailCount = blocked.FailCount;

   if (blocked.IsBlocked && blocked.BlockUntil.HasValue)
{
    model.BlockUntilMs = new DateTimeOffset(blocked.BlockUntil.Value).ToUnixTimeMilliseconds();
    model.RemainingSeconds = (int)Math.Ceiling((blocked.BlockUntil.Value - DateTime.UtcNow).TotalSeconds);

    await _rate.LogAsync(null, model.TenDangNhap, ip, key, endpoint, "POST",
        thanhCong: false, biGioiHan: true, thongBao: blocked.Message);

    // ModelState.AddModelError(string.Empty, $"Bạn chỉ được nhập sai tối đa 5 lần. {blocked.Message}");
    return View(model);
}

    // 2) Login check
    var user = await _db.TaiKhoans
        .FirstOrDefaultAsync(x => x.TenDangNhap == model.TenDangNhap && x.MatKhau == model.MatKhau);

  if (user == null)
{
    await _rate.RegisterFailAsync(key, endpoint);

    var st2 = await _rate.IsBlockedAsync(key, endpoint);
    model.FailCount = st2.FailCount;

    // ✅ nếu vừa sai lần 5 => st2.IsBlocked sẽ TRUE ngay
    if (st2.IsBlocked && st2.BlockUntil.HasValue)
    {
        model.BlockUntilMs = new DateTimeOffset(st2.BlockUntil.Value).ToUnixTimeMilliseconds();
        model.RemainingSeconds = (int)Math.Ceiling((st2.BlockUntil.Value - DateTime.UtcNow).TotalSeconds);

        var msgBlocked = $"Bạn chỉ được nhập sai tối đa 5 lần. Vui lòng thử lại sau {model.RemainingSeconds}s.";
        // ModelState.AddModelError(string.Empty, msgBlocked);
        return View(model);
    }

    int remainTry = Math.Max(0, 5 - model.FailCount);
    var msg = $"Sai tài khoản hoặc mật khẩu. Bạn chỉ được nhập sai tối đa 5 lần. Còn {remainTry} lần.";
    ViewBag.Msg = msg;   // hoặc msgBlocked
return View(model);

}

    // 3) Đúng -> reset
    await _rate.ResetAsync(key, endpoint);

    await _rate.LogAsync(user.Id, model.TenDangNhap, ip, key, endpoint, "POST",
        thanhCong: true, biGioiHan: false, thongBao: "Đăng nhập thành công.");

    // ===== SIGN IN =====
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
