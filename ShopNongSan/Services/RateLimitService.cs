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
       public async Task<(bool IsBlocked, string Message, DateTime? BlockUntil, int FailCount)> IsBlockedAsync(string key, string endpoint)
{
    var now = DateTime.UtcNow;

    var counter = await _db.DemRateLimits
        .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
        .OrderByDescending(x => x.CapNhatLuc)
        .FirstOrDefaultAsync();

    if (counter == null) return (false, "", null, 0);

    // nếu đang trong cửa sổ 60s và đã vượt ngưỡng
    if (counter.KetThucCuaSo > now && counter.SoLuong >= MAX_FAIL)
    {
        var until = counter.KetThucCuaSo;
        var remain = (int)Math.Ceiling((until - now).TotalSeconds);
        return (true, $"Bạn đã nhập sai quá {MAX_FAIL} lần. Vui lòng thử lại sau {remain}s.", until, counter.SoLuong);
    }

    return (false, "", null, counter.SoLuong);
}

public async Task RegisterFailAsync(string key, string endpoint)
{
    var now = DateTime.UtcNow;

    var counter = await _db.DemRateLimits
        .Where(x => x.GiaTriKhoa == key && x.Endpoint == endpoint)
        .OrderByDescending(x => x.CapNhatLuc)
        .FirstOrDefaultAsync();

    // nếu chưa có hoặc đã hết cửa sổ -> tạo cửa sổ mới
    if (counter == null || counter.KetThucCuaSo <= now)
    {
        counter = new DemRateLimit
        {
            GiaTriKhoa = key,
            Endpoint = endpoint,
            BatDauCuaSo = now,
            KetThucCuaSo = now.Add(WINDOW),
            SoLuong = 1,
            CapNhatLuc = now
        };
        _db.DemRateLimits.Add(counter);
        await _db.SaveChangesAsync();
        return;
    }

    // nếu đã đạt ngưỡng rồi thì thôi (đang bị khóa)
    if (counter.SoLuong >= MAX_FAIL && counter.KetThucCuaSo > now)
        return;

    // tăng đếm
    counter.SoLuong += 1;
    counter.CapNhatLuc = now;

    // ✅ Sai lần thứ 5 => KHÓA 60s TỪ LÚC NÀY (luôn hiện ~60s)
    if (counter.SoLuong >= MAX_FAIL)
    {
        counter.BatDauCuaSo = now;
        counter.KetThucCuaSo = now.Add(WINDOW);
    }

    _db.DemRateLimits.Update(counter);
    await _db.SaveChangesAsync();
}



        // gọi khi đăng nhập thành công (reset đếm để không chặn sai quá mức)
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
