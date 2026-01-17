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

        public RateLimitService(NongSanContext db)
        {
            _db = db;
        }

        // tạo khóa rate limit theo username + ip (để không chặn nhầm người khác)
        public static string BuildKey(string? username, string? ip)
            => $"{(username ?? "").Trim().ToLower()}|{(ip ?? "").Trim()}";

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
            if (counter == null || counter.KetThucCuaSo <= now)
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
