using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Policy = "AdminOnly")]
    public class DanhMucsController : Controller
    {
        private readonly NongSanContext _context;

        public DanhMucsController(NongSanContext context)
        {
            _context = context;
        }

        // ===== INDEX =====
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 7)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var query = _context.DanhMucs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                const string AI_COLLATION = "Vietnamese_100_CI_AI";
                query = query.Where(dm => EF.Functions.Like(EF.Functions.Collate(dm.Ten, AI_COLLATION), $"%{key}%"));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(dm => dm.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = pageSize;

            return View(data);
        }

        // ===== CREATE =====
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.DanhMucs.Add(model);
            await _context.SaveChangesAsync();
            SetToast("Tạo danh mục thành công");
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();
            return View(dm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DanhMuc model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();

            dm.Ten = model.Ten;
            await _context.SaveChangesAsync();
            SetToast("Cập nhật danh mục thành công");
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dm = await _context.DanhMucs.FirstOrDefaultAsync(x => x.Id == id);
            if (dm == null) return NotFound();
            return View(dm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();

            _context.DanhMucs.Remove(dm);
            await _context.SaveChangesAsync();
            SetToast("Đã xoá danh mục");
            return RedirectToAction(nameof(Index));
        }

        // Toast helper
        private void SetToast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;
        }
    }
}
