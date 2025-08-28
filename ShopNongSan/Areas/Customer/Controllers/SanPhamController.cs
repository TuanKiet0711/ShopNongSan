using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
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

		// GET: SanPhams
		public async Task<IActionResult> Index()
		{
			var sanPhams = await _context.SanPhams
				.Include(sp => sp.DanhMuc)
				.Include(sp => sp.ThuongHieu)
				.ToListAsync();

			return View(sanPhams);
		}
	}
}
