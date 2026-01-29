using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Services
{
    public class RateLimitService
    {
        private readonly NongSanContext _db;

        // cấu hình yêu cầu: sai 5 lần => chặn 60s
        private const int MAX_FAIL = 5;
        private static readonly TimeSpan WINDOW = TimeSpan.FromSeconds(60);
        private const int LOCKOUT_BASE_SECONDS = 60;
        private const int LOCKOUT_MAX_SECONDS = 60 * 60;

        public RateLimitService(NongSanContext db)
        {
            _db = db;
        }

        // tạo khóa rate limit theo username + ip (để không chặn nhầm người khác)
        public static string BuildKey(string? username, string? ip)
            => $"{(username ?? "").Trim().ToLower()}|{(ip ?? "").Trim()}";

        // tạo khóa theo username-only (tránh false positive khi nhiều người chung IP)
        public static string BuildUserKey(string? username)
            => BuildKey(username, "");

        // kiểm tra đang bị chặn hay không (nếu bị chặn trả về message)
        public Task<(bool IsBlocked, string Message, DateTime? BlockUntil, int FailCount)> IsBlockedAsync(string key, string endpoint)
            => IsBlockedAsync(key, endpoint, MAX_FAIL, WINDOW);

        public async Task<(bool IsBlocked, string Message, DateTime? BlockUntil, int FailCount)> IsBlockedAsync(
            string key, string endpoint, int maxFail, TimeSpan window)
        {
            var now = DateTime.UtcNow;

            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            if (counter == null) return (false, "", null, 0);

            // n?u dang trong c?a s? v… da vu?t ngu?ng
            if (counter.KetThucCuaSo > now && counter.SoLuong >= maxFail)
            {
                var until = counter.KetThucCuaSo;
                var remain = (int)Math.Ceiling((until - now).TotalSeconds);
                return (true, $"B?n da nh?p sai qu  {maxFail} l?n. Vui l•ng th? l?i sau {remain}s.", until, counter.SoLuong);
            }

            return (false, "", null, counter.SoLuong);
        }

        public async Task<int> GetActiveCountAsync(string key, string endpoint)
        {
            var now = DateTime.UtcNow;
            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            if (counter == null || counter.KetThucCuaSo <= now) return 0;
            return counter.SoLuong;
        }

        public async Task<int> GetPersistentCountAsync(string key, string endpoint)
        {
            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            return counter?.SoLuong ?? 0;
        }

        public async Task<(bool IsBlocked, DateTime? BlockUntil, int Level)> IsLockoutAsync(string key, string endpoint)
        {
            var now = DateTime.UtcNow;
            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            if (counter == null) return (false, null, 0);
            if (counter.KetThucCuaSo > now && counter.SoLuong > 0)
                return (true, counter.KetThucCuaSo, counter.SoLuong);

            return (false, null, counter.SoLuong);
        }

        public async Task<(bool IsLocked, DateTime? BlockUntil, int LockoutLevel, int FailCount)> RegisterLoginFailAsync(
            string userKey, string userIpKey, string endpointIp, string endpointUser, string endpointLockout)
        {
            await RegisterCountAsync(userIpKey, endpointIp, MAX_FAIL, WINDOW);
            var userCount = await RegisterPersistentCountAsync(userKey, endpointUser);

            if (userCount >= MAX_FAIL)
            {
                var lockout = await StartLockoutAsync(userKey, endpointLockout);
                await ResetAsync(userKey, endpointUser);
                return (true, lockout.BlockUntil, lockout.Level, MAX_FAIL);
            }

            return (false, null, 0, userCount);
        }

        public async Task ResetLoginAsync(string userKey, string userIpKey, string endpointIp, string endpointUser, string endpointLockout)
        {
            await ResetAsync(userIpKey, endpointIp);
            await ResetAsync(userKey, endpointUser);
            await ResetAsync(userKey, endpointLockout);
        }

        public Task RegisterFailAsync(string key, string endpoint)
            => RegisterCountAsync(key, endpoint, MAX_FAIL, WINDOW);

        public Task RegisterHitAsync(string key, string endpoint, int maxCount, TimeSpan window)
            => RegisterCountAsync(key, endpoint, maxCount, window);

        private async Task RegisterCountAsync(string key, string endpoint, int maxCount, TimeSpan window)
        {
            var now = DateTime.UtcNow;

            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            // n?u chua c¢ ho?c da h?t c?a s? -> t?o c?a s? m?i
            if (counter == null)
            {
                counter = new DemRateLimit
                {
                    GiaTriKhoa = key,
                    Endpoint = endpoint,
                    BatDauCuaSo = now,
                    KetThucCuaSo = now.Add(window),
                    SoLuong = 1,
                    CapNhatLuc = now
                };
                _db.DemRateLimits.Add(counter);
                await _db.SaveChangesAsync();
                return;
            }

            if (counter.KetThucCuaSo <= now)
            {
                // h?t c?a s? nhung v?n gi? so l?n sai ti?p t?c t?nh d?n
                counter.SoLuong += 1;
                counter.BatDauCuaSo = now;
                counter.KetThucCuaSo = now.Add(window);
                counter.CapNhatLuc = now;

                _db.DemRateLimits.Update(counter);
                await _db.SaveChangesAsync();
                return;
            }

            // n?u da d?t ngu?ng r?i th th“i (dang b? kh¢a)
            if (counter.SoLuong >= maxCount && counter.KetThucCuaSo > now)
                return;

            // tang d?m
            counter.SoLuong += 1;
            counter.CapNhatLuc = now;

            // ? Dat ngu?ng => khoa tu luc nay
            if (counter.SoLuong >= maxCount)
            {
                counter.BatDauCuaSo = now;
                counter.KetThucCuaSo = now.Add(window);
            }

            _db.DemRateLimits.Update(counter);
            await _db.SaveChangesAsync();
        }

        private async Task<int> RegisterPersistentCountAsync(string key, string endpoint)
        {
            var now = DateTime.UtcNow;

            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            if (counter == null)
            {
                counter = new DemRateLimit
                {
                    GiaTriKhoa = key,
                    Endpoint = endpoint,
                    BatDauCuaSo = now,
                    KetThucCuaSo = DateTime.MaxValue,
                    SoLuong = 1,
                    CapNhatLuc = now
                };
                _db.DemRateLimits.Add(counter);
                await _db.SaveChangesAsync();
                return counter.SoLuong;
            }

            counter.SoLuong += 1;
            counter.CapNhatLuc = now;

            _db.DemRateLimits.Update(counter);
            await _db.SaveChangesAsync();

            return counter.SoLuong;
        }

        private static int GetLockoutSeconds(int level)
        {
            if (level <= 0) return 0;

            var seconds = LOCKOUT_BASE_SECONDS;
            for (var i = 1; i < level; i++)
            {
                if (seconds >= LOCKOUT_MAX_SECONDS) return LOCKOUT_MAX_SECONDS;
                seconds *= 2;
            }

            return Math.Min(seconds, LOCKOUT_MAX_SECONDS);
        }

        private async Task<(DateTime BlockUntil, int Level)> StartLockoutAsync(string key, string endpoint)
        {
            var now = DateTime.UtcNow;
            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            var level = (counter?.SoLuong ?? 0) + 1;
            var until = now.AddSeconds(GetLockoutSeconds(level));

            if (counter == null)
            {
                counter = new DemRateLimit
                {
                    GiaTriKhoa = key,
                    Endpoint = endpoint,
                    BatDauCuaSo = now,
                    KetThucCuaSo = until,
                    SoLuong = level,
                    CapNhatLuc = now
                };
                _db.DemRateLimits.Add(counter);
            }
            else
            {
                counter.SoLuong = level;
                counter.BatDauCuaSo = now;
                counter.KetThucCuaSo = until;
                counter.CapNhatLuc = now;
                _db.DemRateLimits.Update(counter);
            }

            await _db.SaveChangesAsync();
            return (until, level);
        }

        public async Task ResetAsync(string key, string endpoint)
        {
            var now = DateTime.UtcNow;

            var counter = await _db.DemRateLimits
                .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
                .OrderByDescending(x => x.CapNhatLuc)
                .FirstOrDefaultAsync();

            if (counter == null) return;

            // reset = set SoLuong về 0 và cho hết cửa sổ luôn
            counter.SoLuong = 0;
            counter.BatDauCuaSo = now;
            counter.KetThucCuaSo = now; // hết chặn ngay
            counter.CapNhatLuc = now;

            _db.DemRateLimits.Update(counter);
            await _db.SaveChangesAsync();
        }

        // ghi nhật ký (thành công / bị giới hạn)
        public async Task LogAsync(Guid? taiKhoanId, string? username, string? ip, string key,
            string endpoint, string method, bool thanhCong, bool biGioiHan, string? thongBao)
        {
            var log = new NhatKyDangNhap
            {
                TaiKhoanId = taiKhoanId,
                TenDangNhap = username,
                DiaChiIp = ip,
                KhoaRateLimit = key,
                Endpoint = endpoint,
                PhuongThuc = method,
                ThanhCong = thanhCong,
                BiGioiHan = biGioiHan,
                ThongBao = thongBao,
                ThoiGian = DateTime.UtcNow
            };

            _db.NhatKyDangNhaps.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
