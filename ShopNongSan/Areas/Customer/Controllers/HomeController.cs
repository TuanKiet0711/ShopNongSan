using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using System.Threading.Tasks;
using System.Linq;

namespace ShopNongSan.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly NongSanContext _context;

        public HomeController(NongSanContext context)
        {
            _context = context;
        }

        // Trang chủ: có thể show 8 sản phẩm mới / nổi bật
        public async Task<IActionResult> Index()
        {
            var sanPhamMoi = await _context.SanPhams
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(8)
                .ToListAsync();

            return View(sanPhamMoi);
        }
    }
}
