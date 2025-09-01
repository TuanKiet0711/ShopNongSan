using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    // [Authorize(Policy = "AdminOnly")] // Bật lại khi đã cấu hình auth
    public class SanPhamsController : Controller
    {
        private readonly NongSanContext _context;
        private readonly IWebHostEnvironment _env;

        public SanPhamsController(NongSanContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ====== LIST (Search + Filter + Pagination) ======
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? danhMucId, int? thuongHieuId, int page = 1, int pageSize = 7)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            // nguồn select lọc
            ViewBag.DanhMucId = new SelectList(await _context.DanhMucs.AsNoTracking().ToListAsync(), "Id", "Ten", danhMucId);
            ViewBag.ThuongHieuId = new SelectList(await _context.ThuongHieus.AsNoTracking().ToListAsync(), "Id", "Ten", thuongHieuId);

            // base query
            var query = _context.SanPhams
                .AsNoTracking()
                .Include(s => s.DanhMuc)
                .Include(s => s.ThuongHieu)
                .AsQueryable();

            // =============== SEARCH: gõ KHÔNG DẤU ===============
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();

                // Accent-insensitive collation (ưu tiên Vietnamese_100; nếu không có thì dùng SQL_Latin…)
                const string AI_COLLATION = "Vietnamese_100_CI_AI";
                // Nếu server bạn không hỗ trợ Vietnamese_100_CI_AI, đổi thành:
                // const string AI_COLLATION = "SQL_Latin1_General_CP1_CI_AI";

                query = query.Where(s =>
                    EF.Functions.Like(EF.Functions.Collate(s.Ten, AI_COLLATION), $"%{key}%") ||
                    (s.DanhMuc != null && EF.Functions.Like(EF.Functions.Collate(s.DanhMuc.Ten!, AI_COLLATION), $"%{key}%")) ||
                    (s.ThuongHieu != null && EF.Functions.Like(EF.Functions.Collate(s.ThuongHieu.Ten!, AI_COLLATION), $"%{key}%"))
                );
            }
            // =====================================================

            // filter
            if (danhMucId.HasValue && danhMucId > 0) query = query.Where(s => s.DanhMucId == danhMucId);
            if (thuongHieuId.HasValue && thuongHieuId > 0) query = query.Where(s => s.ThuongHieuId == thuongHieuId);

            // tổng & phân trang
            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // giữ lại trạng thái để View xây URL
            ViewBag.Q = q;
            ViewBag.FilterDanhMucId = danhMucId;
            ViewBag.FilterThuongHieuId = thuongHieuId;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = pageSize;

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
            // Bỏ validate navigation để tránh lỗi required
            ModelState.Remove("DanhMuc");
            ModelState.Remove("ThuongHieu");
            ModelState.Remove("DonHangChiTiets");

            if (model.DanhMucId <= 0)
                ModelState.AddModelError("DanhMucId", "Vui lòng chọn danh mục.");

            await ValidateImage(HinhAnhFile);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                return View(model);
            }

            if (HinhAnhFile is not null && HinhAnhFile.Length > 0)
                model.HinhAnh = await SaveImageAsync(HinhAnhFile);

            try
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                SetToast("Tạo sản phẩm thành công");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu: " + ex.Message);
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                SetToast("Lỗi khi lưu: " + ex.Message, "danger");
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
            // Bỏ validate navigation
            ModelState.Remove("DanhMuc");
            ModelState.Remove("ThuongHieu");
            ModelState.Remove("DonHangChiTiets");

            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();

            await ValidateImage(HinhAnhFile);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.DanhMucId, model.ThuongHieuId);
                return View(model);
            }

            sp.Ten = model.Ten;
            sp.DanhMucId = model.DanhMucId;
            sp.ThuongHieuId = model.ThuongHieuId;
            sp.Gia = model.Gia;
            sp.SoLuongTon = model.SoLuongTon;

            if (HinhAnhFile is not null && HinhAnhFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(sp.HinhAnh)) DeleteImageIfExists(sp.HinhAnh);
                sp.HinhAnh = await SaveImageAsync(HinhAnhFile);
            }

            await _context.SaveChangesAsync();
            SetToast("Cập nhật sản phẩm thành công");
            return RedirectToAction(nameof(Index));
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
            if (sp == null) return NotFound();

            if (!string.IsNullOrEmpty(sp.HinhAnh))
                DeleteImageIfExists(sp.HinhAnh);

            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            SetToast("Đã xoá sản phẩm");
            return RedirectToAction(nameof(Index));
        }

        // ====== Helpers ======
        private async Task LoadDropdowns(int? danhMucId = null, int? thuongHieuId = null)
        {
            ViewBag.DanhMucId = new SelectList(await _context.DanhMucs.AsNoTracking().ToListAsync(), "Id", "Ten", danhMucId);
            ViewBag.ThuongHieuId = new SelectList(await _context.ThuongHieus.AsNoTracking().ToListAsync(), "Id", "Ten", thuongHieuId);
        }

        private async Task ValidateImage(IFormFile? file)
        {
            if (file is null) { await Task.CompletedTask; return; }
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("HinhAnh", "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp, .gif");
            if (file.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("HinhAnh", "Kích thước ảnh tối đa 5MB");
            await Task.CompletedTask;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var root = Path.Combine(_env.WebRootPath, "images"); // wwwroot/images
            Directory.CreateDirectory(root);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(root, fileName);

            using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream);
            }
            return $"/images/{fileName}";
        }

        private void DeleteImageIfExists(string relativePath)
        {
            var physical = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
        }

        // ====== Toast ======
        private void SetToast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;          // Nội dung
            TempData["ToastType"] = type;                // success | danger | warning | info
        }
    }
}
