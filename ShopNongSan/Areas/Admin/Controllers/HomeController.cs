using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Models;
using System.Linq;

namespace ShopNongSan.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly NongSanContext _context;

        public HomeController(NongSanContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalProducts = _context.SanPhams.Count();
            ViewBag.TotalCategories = _context.DanhMucs.Count();
            ViewBag.TotalOrders = _context.DonHangs.Count();
            ViewBag.TotalRevenue = _context.DonHangs.Sum(d => d.TongTien);

            // Gom theo tháng dựa trên NgayDat
            var statsByMonth = _context.DonHangs
                .GroupBy(d => new { d.NgayDat.Year, d.NgayDat.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalRevenue = g.Sum(x => x.TongTien),
                    TotalOrders = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            ViewBag.RevenueLabels = statsByMonth.Select(x => $"{x.Month}/{x.Year}").ToArray();
            ViewBag.RevenueValues = statsByMonth.Select(x => x.TotalRevenue).ToArray();
            ViewBag.OrderValues = statsByMonth.Select(x => x.TotalOrders).ToArray(); // ✅ thêm dòng này

            return View();
        }

    }
}
