using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using ShopNongSan.Services;
using System;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace ShopNongSan.Controllers
{
    [Area("Customer")]
    public class SanPhamsController : Controller
    {
        private readonly NongSanContext _context;
        private readonly RateLimitService _rate;

        private const int SEARCH_MAX = 15;
        private static readonly TimeSpan SEARCH_WINDOW = TimeSpan.FromMinutes(1);

        public SanPhamsController(NongSanContext context, RateLimitService rate)
        {
            _context = context;
            _rate = rate;
        }

        private static decimal? ParseVnd(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return null;
            return decimal.TryParse(digits, out var v) ? v : null;
        }

        // GET: Customer/SanPhams
        public async Task<IActionResult> Index([FromQuery] SanPhamFilterVM filter)
        {
            const string endpoint = "/san-pham";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userKey = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.Identity?.Name;
            var key = RateLimitService.BuildKey(userKey, ip);

            var blocked = await _rate.IsBlockedAsync(key, endpoint, SEARCH_MAX, SEARCH_WINDOW);
            var isBlocked = blocked.IsBlocked && blocked.BlockUntil.HasValue;
            if (isBlocked)
            {
                var untilUtc = DateTime.SpecifyKind(blocked.BlockUntil.Value, DateTimeKind.Utc);
                var remaining = Math.Max(0, (int)Math.Ceiling((untilUtc - DateTime.UtcNow).TotalSeconds));
                var msg = $"\u0110ang b\u1ecb gi\u1edbi h\u1ea1n t\u00ecm ki\u1ebfm, vui l\u00f2ng th\u1eed l\u1ea1i sau {remaining}s.";

                ViewBag.RateLimitMsg = msg;
                ViewBag.RateLimitRemainingSeconds = remaining;

                await _rate.LogAsync(null, User?.Identity?.Name, ip, key, endpoint, "GET",
                    thanhCong: false, biGioiHan: true, thongBao: msg);
            }
            else
            {
                await _rate.RegisterHitAsync(key, endpoint, SEARCH_MAX, SEARCH_WINDOW);
            }

            var query = _context.SanPhams
                .Include(sp => sp.DanhMuc)
                .Include(sp => sp.ThuongHieu)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                query = query.Where(sp =>
                    EF.Functions.Like(
                        EF.Functions.Collate(sp.Ten, "SQL_Latin1_General_CP1_CI_AI"),
                        $"%{q}%"));
            }

            if (filter.DanhMucId.HasValue)
                query = query.Where(sp => sp.DanhMucId == filter.DanhMucId.Value);

            if (filter.ThuongHieuId.HasValue)
                query = query.Where(sp => sp.ThuongHieuId == filter.ThuongHieuId.Value);

            var giaMin = ParseVnd(filter.GiaMinStr);
            var giaMax = ParseVnd(filter.GiaMaxStr);
            if (giaMin.HasValue) query = query.Where(sp => sp.Gia >= giaMin.Value);
            if (giaMax.HasValue) query = query.Where(sp => sp.Gia <= giaMax.Value);

            query = filter.Sort switch
            {
                "price_asc" => query.OrderBy(sp => sp.Gia),
                "price_desc" => query.OrderByDescending(sp => sp.Gia),
                "newest" => query.OrderByDescending(sp => sp.Id),
                _ => query.OrderBy(sp => sp.Ten)
            };

            ViewBag.DanhMucList = new SelectList(await _context.DanhMucs.AsNoTracking().ToListAsync(), "Id", "Ten", filter.DanhMucId);
            ViewBag.ThuongHieuList = new SelectList(await _context.ThuongHieus.AsNoTracking().ToListAsync(), "Id", "Ten", filter.ThuongHieuId);
            ViewBag.Filter = filter;

            var sanPhams = await query.ToListAsync();
            return View(sanPhams);
        }

        // GET: Customer/SanPhams/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sp = await _context.SanPhams
                .Include(x => x.DanhMuc)
                .Include(x => x.ThuongHieu)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sp == null) return NotFound();

            // Liên quan: cùng danh mục, ưu tiên cùng thương hiệu
            var lienQuan = await _context.SanPhams
                .Where(x => x.Id != sp.Id && x.DanhMucId == sp.DanhMucId)
                .OrderByDescending(x => x.ThuongHieuId == sp.ThuongHieuId)
                .ThenByDescending(x => x.Id)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.LienQuan = lienQuan;
            return View(sp);
        }
    }
}
