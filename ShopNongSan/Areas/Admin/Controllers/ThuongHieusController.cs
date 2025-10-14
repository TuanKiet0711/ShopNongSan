using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ThuongHieusController : Controller
    {
        private readonly NongSanContext _context;

        public ThuongHieusController(NongSanContext context)
        {
            _context = context;
        }

        // ===== INDEX =====
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 7)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var query = _context.ThuongHieus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                const string AI_COLLATION = "Vietnamese_100_CI_AI";
                query = query.Where(th => EF.Functions.Like(EF.Functions.Collate(th.Ten, AI_COLLATION), $"%{key}%"));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(th => th.Id)
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
        public async Task<IActionResult> Create(ThuongHieu model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                _context.ThuongHieus.Add(model);
                await _context.SaveChangesAsync();
                SetToast("Tạo thương hiệu thành công");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetToast("Lỗi khi tạo: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ===== EDIT =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var th = await _context.ThuongHieus.FindAsync(id);
            if (th == null) return NotFound();
            return View(th);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThuongHieu model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var th = await _context.ThuongHieus.FindAsync(id);
            if (th == null) return NotFound();

            try
            {
                th.Ten = model.Ten;
                await _context.SaveChangesAsync();
                SetToast("Cập nhật thương hiệu thành công");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetToast("Lỗi khi cập nhật: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ===== DELETE =====
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var th = await _context.ThuongHieus.FirstOrDefaultAsync(x => x.Id == id);
            if (th == null) return NotFound();
            return View(th);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var th = await _context.ThuongHieus.FindAsync(id);
            if (th == null) return NotFound();

            try
            {
                _context.ThuongHieus.Remove(th);
                await _context.SaveChangesAsync();
                SetToast("Đã xoá thương hiệu");
            }
            catch (Exception ex)
            {
                SetToast("Không thể xoá: " + ex.Message, "danger");
            }
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
