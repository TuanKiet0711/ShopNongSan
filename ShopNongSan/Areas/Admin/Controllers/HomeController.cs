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

            // Gửi dữ liệu cho biểu đồ (1 giá trị duy nhất)
            ViewBag.RevenueLabels = new string[] { "Tổng doanh thu" };
            ViewBag.RevenueValues = new decimal[] { ViewBag.TotalRevenue };

            return View();
        }
    }
}
