using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TaiKhoansController : Controller
    {
        private readonly NongSanContext _context;

        public TaiKhoansController(NongSanContext context)
        {
            _context = context;
        }

        // ================= Helpers =================
        private static List<string> RoleList() => new() { "Customer", "Staff", "Admin" };

        // Trả về chuỗi vai trò chuẩn hoá đúng với DB, hoặc null nếu không hợp lệ
        private string? NormalizeAndValidateRole(string? role)
        {
            var allowed = RoleList();
            var raw = (role ?? string.Empty).Trim();
            var hit = allowed.FirstOrDefault(x =>
                string.Equals(x, raw, StringComparison.OrdinalIgnoreCase));
            return hit; // null nếu không khớp
        }

        private void SetToast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type; // success | danger | warning | info
        }

        // ================= INDEX =================
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? vaiTro, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.TaiKhoans.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                const string AI_COLLATION = "Vietnamese_100_CI_AI";
                query = query.Where(tk =>
                    EF.Functions.Like(EF.Functions.Collate(tk.TenDangNhap, AI_COLLATION), $"%{key}%") ||
                    EF.Functions.Like(EF.Functions.Collate(tk.HoTen, AI_COLLATION), $"%{key}%"));
            }

            if (!string.IsNullOrWhiteSpace(vaiTro))
                query = query.Where(tk => tk.VaiTro == vaiTro);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(tk => tk.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.VaiTro = vaiTro;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = pageSize;
            ViewBag.VaiTroList = RoleList();

            return View(data);
        }

        // ================= CREATE =================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.VaiTroList = RoleList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoan model)
        {
            // Bỏ validate navigation
            ModelState.Remove(nameof(TaiKhoan.DonHangs));
            ModelState.Remove(nameof(TaiKhoan.ThongTinNguoiDung));

            // Chuẩn hoá & validate VaiTro
            var canonical = NormalizeAndValidateRole(model.VaiTro);
            if (canonical == null)
            {
                ModelState.AddModelError(nameof(TaiKhoan.VaiTro), "Role must be one of: Customer, Staff, Admin.");
            }
            else
            {
                model.VaiTro = canonical;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.VaiTroList = RoleList();
                return View(model);
            }

            try
            {
                model.Id = Guid.NewGuid();
                model.NgayTao = DateTime.Now;

                _context.TaiKhoans.Add(model);
                await _context.SaveChangesAsync();

                SetToast("Tạo tài khoản thành công");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.VaiTroList = RoleList();
                SetToast("Lỗi khi tạo: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ================= EDIT =================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null) return NotFound();

            ViewBag.VaiTroList = RoleList();
            return View(tk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaiKhoan model)
        {
            // Bỏ validate navigation
            ModelState.Remove(nameof(TaiKhoan.DonHangs));
            ModelState.Remove(nameof(TaiKhoan.ThongTinNguoiDung));

            var canonical = NormalizeAndValidateRole(model.VaiTro);
            if (canonical == null)
            {
                ModelState.AddModelError(nameof(TaiKhoan.VaiTro), "Role must be one of: Customer, Staff, Admin.");
            }
            else
            {
                model.VaiTro = canonical;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.VaiTroList = RoleList();
                return View(model);
            }

            var tk = await _context.TaiKhoans.FindAsync(model.Id);
            if (tk == null) return NotFound();

            try
            {
                tk.TenDangNhap = model.TenDangNhap;
                tk.MatKhau = model.MatKhau; // theo yêu cầu: plain text
                tk.HoTen = model.HoTen;
                tk.VaiTro = model.VaiTro;

                await _context.SaveChangesAsync();
                SetToast("Cập nhật tài khoản thành công");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.VaiTroList = RoleList();
                SetToast("Lỗi khi cập nhật: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ================= DELETE =================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tk = await _context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (tk == null) return NotFound();
            return View(tk);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null) return NotFound();

            try
            {
                _context.TaiKhoans.Remove(tk);
                await _context.SaveChangesAsync();
                SetToast("Đã xoá tài khoản");
            }
            catch (Exception ex)
            {
                SetToast("Không thể xoá: " + ex.Message, "danger");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
