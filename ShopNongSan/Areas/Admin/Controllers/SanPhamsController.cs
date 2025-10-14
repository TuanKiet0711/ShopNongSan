using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using ShopNongSan.Models;
using System.IO;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class SanPhamsController : Controller
    {
        private readonly NongSanContext _context;
        private readonly IWebHostEnvironment _env;

        public SanPhamsController(NongSanContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ====== LIST ======
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? danhMucId, int? thuongHieuId, int page = 1, int pageSize = 7)
        {
            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.ThuongHieu)
                .AsNoTracking()
                .AsQueryable();

            const string AI_COLLATION = "Vietnamese_100_CI_AI";

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(EF.Functions.Collate(s.Ten, AI_COLLATION), $"%{key}%") ||
                    (s.MoTa != null && EF.Functions.Like(EF.Functions.Collate(s.MoTa!, AI_COLLATION), $"%{key}%")) ||
                    (s.DanhMuc != null && EF.Functions.Like(EF.Functions.Collate(s.DanhMuc.Ten!, AI_COLLATION), $"%{key}%")) ||
                    (s.ThuongHieu != null && EF.Functions.Like(EF.Functions.Collate(s.ThuongHieu.Ten!, AI_COLLATION), $"%{key}%"))
                );
            }

            if (danhMucId.HasValue && danhMucId > 0)
                query = query.Where(s => s.DanhMucId == danhMucId);
            if (thuongHieuId.HasValue && thuongHieuId > 0)
                query = query.Where(s => s.ThuongHieuId == thuongHieuId);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var data = await query.OrderByDescending(s => s.Id)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.DanhMucId = new SelectList(await _context.DanhMucs.ToListAsync(), "Id", "Ten", danhMucId);
            ViewBag.ThuongHieuId = new SelectList(await _context.ThuongHieus.ToListAsync(), "Id", "Ten", thuongHieuId);

            return View(data);
        }

        // ====== CREATE ======
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPham model, IFormFile? HinhAnhFile)
        {
            ModelState.Remove("DanhMuc");
            ModelState.Remove("ThuongHieu");

            if (!string.IsNullOrEmpty(model.MoTa) && model.MoTa.Length > 500)
                ModelState.AddModelError("MoTa", "Mô tả tối đa 500 ký tự.");

            await ValidateImage(HinhAnhFile);
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                SetToast("Vui lòng kiểm tra lại thông tin.", "warning");
                return View(model);
            }

            try
            {
                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                    model.HinhAnh = await SaveImageAsync(HinhAnhFile);

                _context.Add(model);
                await _context.SaveChangesAsync();

                SetToast("Thêm sản phẩm thành công", "success");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                SetToast("Lỗi khi tạo: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ====== EDIT ======
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();
            await LoadDropdowns(sp.DanhMucId, sp.ThuongHieuId);
            return View(sp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SanPham model, IFormFile? HinhAnhFile)
        {
            ModelState.Remove("DanhMuc");
            ModelState.Remove("ThuongHieu");

            if (!string.IsNullOrEmpty(model.MoTa) && model.MoTa.Length > 500)
                ModelState.AddModelError("MoTa", "Mô tả tối đa 500 ký tự.");

            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();

            await ValidateImage(HinhAnhFile);
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                SetToast("Vui lòng kiểm tra lại thông tin.", "warning");
                return View(model);
            }

            try
            {
                sp.Ten = model.Ten;
                sp.Gia = model.Gia;
                sp.DanhMucId = model.DanhMucId;
                sp.ThuongHieuId = model.ThuongHieuId;
                sp.SoLuongTon = model.SoLuongTon;
                sp.MoTa = model.MoTa;

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrWhiteSpace(sp.HinhAnh))
                        DeleteImageIfExists(sp.HinhAnh);

                    sp.HinhAnh = await SaveImageAsync(HinhAnhFile);
                }

                await _context.SaveChangesAsync();
                SetToast("Cập nhật sản phẩm thành công", "success");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                SetToast("Lỗi khi cập nhật: " + ex.Message, "danger");
                return View(model);
            }
        }

        // ====== DELETE ======
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var sp = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.ThuongHieu)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sp == null) return NotFound();
            return View(sp);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null)
            {
                SetToast("Sản phẩm không tồn tại.", "warning");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(sp.HinhAnh))
                    DeleteImageIfExists(sp.HinhAnh);

                _context.SanPhams.Remove(sp);
                await _context.SaveChangesAsync();
                SetToast("Đã xoá sản phẩm", "success");
            }
            catch (Exception ex)
            {
                SetToast("Không thể xoá: " + ex.Message, "danger");
            }

            return RedirectToAction(nameof(Index));
        }

        // ====== Helpers ======
        private async Task LoadDropdowns(int? dm = null, int? th = null)
        {
            ViewBag.DanhMucId = new SelectList(await _context.DanhMucs.ToListAsync(), "Id", "Ten", dm);
            ViewBag.ThuongHieuId = new SelectList(await _context.ThuongHieus.ToListAsync(), "Id", "Ten", th);
        }

        private async Task ValidateImage(IFormFile? f)
        {
            if (f == null) return;
            var allowed = new[] { ".jpg", ".png", ".jpeg", ".gif", ".webp" };
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("HinhAnh", "Chỉ cho phép ảnh .jpg, .png, .gif, .webp");
            if (f.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("HinhAnh", "Tối đa 5MB");
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var root = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(root);
            var fn = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(root, fn);
            using (var s = System.IO.File.Create(path))
                await file.CopyToAsync(s);
            return $"/images/{fn}";
        }

        private void DeleteImageIfExists(string relativePath)
        {
            try
            {
                var physical = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }
            catch
            {
                // bỏ qua nếu không xóa được, tránh crash
            }
        }

        // ====== Toast ======
        private void SetToast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;          // Nội dung
            TempData["ToastType"] = type;                // success | danger | warning | info
        }
    }
}
