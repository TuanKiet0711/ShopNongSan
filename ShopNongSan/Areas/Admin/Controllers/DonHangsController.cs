using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _context;

        public DonHangsController(NongSanContext context)
        {
            _context = context;
        }

        // ===== INDEX =====
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? trangThai, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.DonHangs
                .Include(d => d.TaiKhoan)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                const string AI_COLLATION = "Vietnamese_100_CI_AI";
                query = query.Where(dh => EF.Functions.Like(EF.Functions.Collate(dh.MaDonHang, AI_COLLATION), $"%{key}%"));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(dh => dh.TrangThai == trangThai);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(dh => dh.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.TrangThai = trangThai;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = pageSize;

            ViewBag.TrangThaiList = new List<string> { "Chờ xử lý", "Đang giao", "Hoàn tất", "Đã huỷ" };

            return View(data);
        }

        // ===== CREATE =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonHang model)
        {
            ModelState.Remove("TaiKhoan");
            ModelState.Remove("DonHangChiTiets");
            ModelState.Remove("DoiTras");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.TaiKhoanId);
                return View(model);
            }

            model.MaDonHang = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"); // auto gen code
            _context.DonHangs.Add(model);
            await _context.SaveChangesAsync();
            SetToast("Tạo đơn hàng thành công");
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT =====
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound();

            await LoadDropdowns(dh.TaiKhoanId);
            return View(dh);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, DonHang model)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove("TaiKhoan");
            ModelState.Remove("DonHangChiTiets");
            ModelState.Remove("DoiTras");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.TaiKhoanId);
                return View(model);
            }

            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound();

            dh.TaiKhoanId = model.TaiKhoanId;
            dh.TongTien = model.TongTien;
            dh.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            SetToast("Cập nhật đơn hàng thành công");
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var dh = await _context.DonHangs
                .Include(d => d.TaiKhoan)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dh == null) return NotFound();
            return View(dh);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound();

            _context.DonHangs.Remove(dh);
            await _context.SaveChangesAsync();
            SetToast("Đã xoá đơn hàng");
            return RedirectToAction(nameof(Index));
        }

        // ===== Helpers =====
        private async Task LoadDropdowns(Guid? selectedTaiKhoanId = null)
        {
            var listTK = await _context.TaiKhoans.AsNoTracking().ToListAsync();
            ViewBag.TaiKhoanId = new SelectList(listTK, "Id", "Email", selectedTaiKhoanId); // giả sử TK có Email
            ViewBag.TrangThaiList = new List<string> { "Chờ xử lý", "Đang giao", "Hoàn tất", "Đã huỷ" };
        }

        private void SetToast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;
        }
    }
}
