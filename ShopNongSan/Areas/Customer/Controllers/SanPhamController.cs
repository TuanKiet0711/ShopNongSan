using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace ShopNongSan.Controllers
{
    [Area("Customer")]
    public class SanPhamsController : Controller
    {
        private readonly NongSanContext _context;

        public SanPhamsController(NongSanContext context)
        {
            _context = context;
        }

        // Parse "1,500,000 đ" -> 1500000
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
            var query = _context.SanPhams
                .Include(sp => sp.DanhMuc)
                .Include(sp => sp.ThuongHieu)
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KHÔNG DẤU (accent-insensitive) =====
            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                // Dùng collation phổ biến: SQL_Latin1_General_CP1_CI_AI
                query = query.Where(sp =>
                    EF.Functions.Like(
                        EF.Functions.Collate(sp.Ten, "SQL_Latin1_General_CP1_CI_AI"),
                        $"%{q}%")
                );
            }

            if (filter.DanhMucId.HasValue)
                query = query.Where(sp => sp.DanhMucId == filter.DanhMucId.Value);

            if (filter.ThuongHieuId.HasValue)
                query = query.Where(sp => sp.ThuongHieuId == filter.ThuongHieuId.Value);

            // ===== KHOẢNG GIÁ =====
            var giaMin = ParseVnd(filter.GiaMinStr);
            var giaMax = ParseVnd(filter.GiaMaxStr);

            if (giaMin.HasValue) query = query.Where(sp => sp.Gia >= giaMin.Value);
            if (giaMax.HasValue) query = query.Where(sp => sp.Gia <= giaMax.Value);

            // Sắp xếp
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
    }
}
