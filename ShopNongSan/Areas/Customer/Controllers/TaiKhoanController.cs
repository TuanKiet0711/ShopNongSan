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

        private const int MAX_FAIL = 5;

        public TaiKhoanController(NongSanContext db, RateLimitService rate)
        {
            _db = db;
            _rate = rate;
        }

        // GET: /Customer/TaiKhoan/DangNhap
        [HttpGet, AllowAnonymous]
        public IActionResult DangNhap(string? returnUrl = null)
        {
            // TempData không serialize Int64 trong cấu hình mặc định -> lưu dạng string và parse lại
            long blockMs = 0;
            var blockMsStr = TempData["BlockUntilMs"] as string;
            if (!string.IsNullOrWhiteSpace(blockMsStr))
                long.TryParse(blockMsStr, out blockMs);

            var vm = new DangNhapVM
            {
                ReturnUrl = returnUrl,
                TenDangNhap = TempData["TenDangNhap"] as string ?? "",
                FailCount = TempData["FailCount"] as int? ?? 0,
                RemainingSeconds = TempData["RemainingSeconds"] as int? ?? 0,
                BlockUntilMs = blockMs
            };

            ViewBag.Msg = TempData["Msg"];
            return View(vm);
        }

        // POST: /Customer/TaiKhoan/DangNhap
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapVM model)
        {
            if (!ModelState.IsValid)
            {
                // validation field (required) -> return view trực tiếp vẫn OK
                return View(model);
            }

            string endpoint = "/tai-khoan/dang-nhap";
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string key = RateLimitService.BuildKey(model.TenDangNhap, ip);

            // 1) Check đang bị chặn không
            var blocked = await _rate.IsBlockedAsync(key, endpoint);
            model.FailCount = blocked.FailCount;

            if (blocked.IsBlocked && blocked.BlockUntil.HasValue)
            {
                // FIX timezone: datetime SQL -> Kind Unspecified, ép UTC trước khi convert
                var untilUtc = DateTime.SpecifyKind(blocked.BlockUntil.Value, DateTimeKind.Utc);

                model.BlockUntilMs = new DateTimeOffset(untilUtc).ToUnixTimeMilliseconds();
                model.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((untilUtc - DateTime.UtcNow).TotalSeconds));

                await _rate.LogAsync(null, model.TenDangNhap, ip, key, endpoint, "POST",
                    thanhCong: false, biGioiHan: true, thongBao: blocked.Message);

                TempData["Msg"] = $"Tài khoản \"{model.TenDangNhap}\" đang bị khóa. Vui lòng thử lại sau {model.RemainingSeconds}s.";
                TempData["TenDangNhap"] = model.TenDangNhap;
                TempData["FailCount"] = model.FailCount;
                TempData["RemainingSeconds"] = model.RemainingSeconds;

                // ✅ lưu string để không bị lỗi serialize Int64
                TempData["BlockUntilMs"] = model.BlockUntilMs.ToString();

                return RedirectToAction(nameof(DangNhap), new { returnUrl = model.ReturnUrl });
            }

            // 2) Login check
            var user = await _db.TaiKhoans
                .FirstOrDefaultAsync(x => x.TenDangNhap == model.TenDangNhap && x.MatKhau == model.MatKhau);

            if (user == null)
            {
                // Sai -> tăng đếm
                await _rate.RegisterFailAsync(key, endpoint);

                // lấy lại trạng thái sau khi tăng fail
                var st2 = await _rate.IsBlockedAsync(key, endpoint);
                model.FailCount = st2.FailCount;

                // nếu vừa sai lần 5 => bị chặn ngay
                if (st2.IsBlocked && st2.BlockUntil.HasValue)
                {
                    var untilUtc2 = DateTime.SpecifyKind(st2.BlockUntil.Value, DateTimeKind.Utc);

                    model.BlockUntilMs = new DateTimeOffset(untilUtc2).ToUnixTimeMilliseconds();
                    model.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((untilUtc2 - DateTime.UtcNow).TotalSeconds));

                    var msgBlocked = $"Tài khoản \"{model.TenDangNhap}\" đang bị khóa. Vui lòng thử lại sau {model.RemainingSeconds}s.";

                    await _rate.LogAsync(null, model.TenDangNhap, ip, key, endpoint, "POST",
                        thanhCong: false, biGioiHan: true, thongBao: msgBlocked);

                    TempData["Msg"] = msgBlocked;
                    TempData["TenDangNhap"] = model.TenDangNhap;
                    TempData["FailCount"] = model.FailCount;
                    TempData["RemainingSeconds"] = model.RemainingSeconds;
                    TempData["BlockUntilMs"] = model.BlockUntilMs.ToString(); // ✅ string

                    return RedirectToAction(nameof(DangNhap), new { returnUrl = model.ReturnUrl });
                }

                // Chưa bị khóa -> báo còn bao nhiêu lần
                int remainTry = Math.Max(0, MAX_FAIL - model.FailCount);
                var msg = $"Sai tài khoản hoặc mật khẩu. Bạn chỉ được nhập sai tối đa {MAX_FAIL} lần. Còn {remainTry} lần.";

                await _rate.LogAsync(null, model.TenDangNhap, ip, key, endpoint, "POST",
                    thanhCong: false, biGioiHan: false, thongBao: msg);

                TempData["Msg"] = msg;
                TempData["TenDangNhap"] = model.TenDangNhap;
                TempData["FailCount"] = model.FailCount;

                // không cần countdown -> xóa cho sạch (tùy chọn)
                TempData.Remove("RemainingSeconds");
                TempData.Remove("BlockUntilMs");

                return RedirectToAction(nameof(DangNhap), new { returnUrl = model.ReturnUrl });
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

            // Auto login sau đăng ký
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
