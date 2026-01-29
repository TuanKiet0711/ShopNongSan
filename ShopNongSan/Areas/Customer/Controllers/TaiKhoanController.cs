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
        private const string LOGIN_ENDPOINT = "/tai-khoan/dang-nhap";
        private const string LOGIN_USER_ENDPOINT = "/tai-khoan/dang-nhap:user";
        private const string LOGIN_LOCKOUT_ENDPOINT = "/tai-khoan/dang-nhap:lockout";
        private const string REGISTER_ENDPOINT = "/tai-khoan/dang-ky";
        private const int REGISTER_MAX = 5;
        private static readonly TimeSpan REGISTER_WINDOW = TimeSpan.FromMinutes(1);
        private const string REGISTER_CAPTCHA_TEMP = "RegisterCaptchaText";

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

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userKey = RateLimitService.BuildUserKey(model.TenDangNhap);
            string ipKey = RateLimitService.BuildKey(model.TenDangNhap, ip);

            // 1) Check lockout theo username-only
            var lockout = await _rate.IsLockoutAsync(userKey, LOGIN_LOCKOUT_ENDPOINT);
            if (lockout.IsBlocked && lockout.BlockUntil.HasValue)
            {
                var untilUtc = DateTime.SpecifyKind(lockout.BlockUntil.Value, DateTimeKind.Utc);

                model.BlockUntilMs = new DateTimeOffset(untilUtc).ToUnixTimeMilliseconds();
                model.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((untilUtc - DateTime.UtcNow).TotalSeconds));
                model.FailCount = await _rate.GetPersistentCountAsync(userKey, LOGIN_USER_ENDPOINT);

                var msgLocked = $"Tài khoản \"{model.TenDangNhap}\" đang bị khóa. Vui lòng thử lại sau {model.RemainingSeconds}s.";

                await _rate.LogAsync(null, model.TenDangNhap, ip, userKey, LOGIN_LOCKOUT_ENDPOINT, "POST",
                    thanhCong: false, biGioiHan: true, thongBao: msgLocked);

                TempData["Msg"] = msgLocked;
                TempData["TenDangNhap"] = model.TenDangNhap;
                TempData["FailCount"] = model.FailCount;
                TempData["RemainingSeconds"] = model.RemainingSeconds;
                TempData["BlockUntilMs"] = model.BlockUntilMs.ToString();

                return RedirectToAction(nameof(DangNhap), new { returnUrl = model.ReturnUrl });
            }

            // 2) Check rate limit ngắn theo username+ip
            var blocked = await _rate.IsBlockedAsync(ipKey, LOGIN_ENDPOINT);
            model.FailCount = await _rate.GetPersistentCountAsync(userKey, LOGIN_USER_ENDPOINT);

            if (blocked.IsBlocked && blocked.BlockUntil.HasValue)
            {
                // FIX timezone: datetime SQL -> Kind Unspecified, ép UTC trước khi convert
                var untilUtc = DateTime.SpecifyKind(blocked.BlockUntil.Value, DateTimeKind.Utc);

                model.BlockUntilMs = new DateTimeOffset(untilUtc).ToUnixTimeMilliseconds();
                model.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((untilUtc - DateTime.UtcNow).TotalSeconds));

                var msgBlocked = $"Tài khoản \"{model.TenDangNhap}\" đang bị khóa. Vui lòng thử lại sau {model.RemainingSeconds}s.";

                await _rate.LogAsync(null, model.TenDangNhap, ip, ipKey, LOGIN_ENDPOINT, "POST",
                    thanhCong: false, biGioiHan: true, thongBao: msgBlocked);

                TempData["Msg"] = msgBlocked;
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
                // Sai -> tăng đếm (username+ip + username-only)
                var failResult = await _rate.RegisterLoginFailAsync(
                    userKey, ipKey, LOGIN_ENDPOINT, LOGIN_USER_ENDPOINT, LOGIN_LOCKOUT_ENDPOINT);
                model.FailCount = failResult.FailCount;

                // nếu vừa sai lần 5 => bị khóa theo level
                if (failResult.IsLocked && failResult.BlockUntil.HasValue)
                {
                    var untilUtc2 = DateTime.SpecifyKind(failResult.BlockUntil.Value, DateTimeKind.Utc);

                    model.BlockUntilMs = new DateTimeOffset(untilUtc2).ToUnixTimeMilliseconds();
                    model.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((untilUtc2 - DateTime.UtcNow).TotalSeconds));

                    var msgBlocked = $"Tài khoản \"{model.TenDangNhap}\" đang bị khóa. Vui lòng thử lại sau {model.RemainingSeconds}s.";

                    await _rate.LogAsync(null, model.TenDangNhap, ip, userKey, LOGIN_LOCKOUT_ENDPOINT, "POST",
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

                await _rate.LogAsync(null, model.TenDangNhap, ip, ipKey, LOGIN_ENDPOINT, "POST",
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
            await _rate.ResetLoginAsync(userKey, ipKey, LOGIN_ENDPOINT, LOGIN_USER_ENDPOINT, LOGIN_LOCKOUT_ENDPOINT);

            await _rate.LogAsync(user.Id, model.TenDangNhap, ip, ipKey, LOGIN_ENDPOINT, "POST",
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
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string ipKey = RateLimitService.BuildKey(null, ip);
            var blocked = await _rate.IsBlockedAsync(ipKey, REGISTER_ENDPOINT, REGISTER_MAX, REGISTER_WINDOW);
            var activeCount = await _rate.GetActiveCountAsync(ipKey, REGISTER_ENDPOINT);
            bool requireCaptcha = activeCount >= 2;
            if (requireCaptcha)
            {
                if (!TryValidateRegisterCaptcha(model.CaptchaInput, out var captchaError))
                {
                    ModelState.AddModelError(nameof(model.CaptchaInput), captchaError);

                    ShowRegisterCaptcha();
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {
                if (requireCaptcha) ShowRegisterCaptcha();
                return View(model);
            }

            await _rate.RegisterHitAsync(ipKey, REGISTER_ENDPOINT, REGISTER_MAX, REGISTER_WINDOW);

            var ten = (model.TenDangNhap ?? "").Trim();
            bool existed = await _db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten);
            if (existed)
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                if (requireCaptcha) ShowRegisterCaptcha();
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

        private void ShowRegisterCaptcha()
        {
            var code = GenerateRegisterCaptchaCode();
            TempData[REGISTER_CAPTCHA_TEMP] = code;
            ViewBag.CaptchaEnabled = true;
            ViewBag.CaptchaCode = code;
        }

        private static string GenerateRegisterCaptchaCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> buffer = stackalloc char[5];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }
            return new string(buffer);
        }

        private bool TryValidateRegisterCaptcha(string? input, out string error)
        {
            error = "Vui lòng nhập mã CAPTCHA.";
            var code = TempData[REGISTER_CAPTCHA_TEMP] as string;
            if (string.IsNullOrWhiteSpace(code))
            {
                error = "CAPTCHA đã hết hạn, vui lòng thử lại.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Vui lòng nhập mã CAPTCHA.";
                return false;
            }

            if (!string.Equals(input.Trim(), code, StringComparison.Ordinal))
            {
                error = "CAPTCHA không đúng.";
                return false;
            }

            return true;
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
