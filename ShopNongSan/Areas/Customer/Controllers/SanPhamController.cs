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
        public SanPhamsController(NongSanContext context) => _context = context;

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
