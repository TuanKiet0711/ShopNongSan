// Areas/Customer/Controllers/HomeController.cs
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

        // Trang chủ: show 8 sp mới
        public async Task<IActionResult> Index()
        {
            var sanPhamMoi = await _context.SanPhams
                .AsNoTracking()
                .Include(x => x.DanhMuc)       // <<< thêm
                .Include(x => x.ThuongHieu)    // <<< thêm
                .OrderByDescending(x => x.Id)
                .Take(8)
                .ToListAsync();

            return View(sanPhamMoi);
        }
    }
}
