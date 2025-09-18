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

            // === Gom theo ngày (7 ngày gần nhất) ===
            var today = DateTime.Today;
            var startDate = today.AddDays(-6);

            var statsByDay = _context.DonHangs
                .Where(d => d.NgayDat.Date >= startDate && d.TrangThai != "Cancelled")
                .GroupBy(d => d.NgayDat.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(x => x.TongTien),
                    TotalOrders = g.Count()
                })
                .ToList();

            // Danh sách đủ 7 ngày liên tục
            var labels = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i))
                .ToList();

            ViewBag.RevenueLabels = labels.Select(d => d.ToString("dd/MM")).ToArray();
            ViewBag.RevenueValues = labels.Select(d =>
                statsByDay.FirstOrDefault(x => x.Date == d)?.TotalRevenue ?? 0
            ).ToArray();
            ViewBag.OrderValues = labels.Select(d =>
                statsByDay.FirstOrDefault(x => x.Date == d)?.TotalOrders ?? 0
            ).ToArray();

            return View();
        }
    }
}
