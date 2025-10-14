using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    [Route("Admin/[controller]")]
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _db;
        public DonHangsController(NongSanContext db) => _db = db;

        // Khớp với CHECK constraint trong DB (tiếng Việt)
        private static readonly string[] AllowedStatuses =
            new[] { "Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn tất", "Đã hủy" };

        // GET: /Admin/DonHangs
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, string? q, int page = 1, int pageSize = 8)
        {
            var query = _db.DonHangs
                .AsNoTracking()
                .Include(d => d.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.TrangThai == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(d =>
                       d.MaDonHang.Contains(q)
                    || (d.HoTen != null && d.HoTen.Contains(q))
                    || (d.TaiKhoan != null && d.TaiKhoan.HoTen.Contains(q))
                );
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            page = Math.Clamp(page, 1, totalPages);

            var items = await query
                .OrderByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Q = q ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.StatusList = new SelectList(AllowedStatuses, status);

            return View(items);
        }

        // GET: /Admin/DonHangs/Details/5
        [HttpGet("Details/{id:long}")]
        public async Task<IActionResult> Details(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (don == null) return NotFound();
            return View(don);
        }

        // GET: /Admin/DonHangs/Edit/5
        [HttpGet("Edit/{id:long}")]
        public async Task<IActionResult> Edit(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();

            ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
            return View(don);
        }

        // POST: /Admin/DonHangs/Edit/5
        [HttpPost("Edit/{id:long}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, string trangThai)
        {
            if (!AllowedStatuses.Contains(trangThai))
                ModelState.AddModelError(nameof(trangThai), "Trạng thái không hợp lệ.");

            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
                return View(don);
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Sang "Đã hủy" (từ trạng thái khác) → cộng kho
                if (trangThai == "Đã hủy" &&
                    !string.Equals(don.TrangThai, "Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ct in don.DonHangChiTiets)
                        if (ct.SanPham != null)
                            ct.SanPham.SoLuongTon += ct.SoLuong;
                }

                // Bỏ "Đã hủy" → trừ kho lại (nếu đủ)
                if (string.Equals(don.TrangThai, "Đã hủy", StringComparison.OrdinalIgnoreCase) &&
                    trangThai != "Đã hủy")
                {
                    foreach (var ct in don.DonHangChiTiets)
                    {
                        if (ct.SanPham == null) continue;
                        if (ct.SanPham.SoLuongTon < ct.SoLuong)
                        {
                            ModelState.AddModelError("", $"Sản phẩm \"{ct.SanPham.Ten}\" không đủ tồn kho để gỡ hủy.");
                            ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
                            return View(don);
                        }
                    }
                    foreach (var ct in don.DonHangChiTiets)
                        if (ct.SanPham != null)
                            ct.SanPham.SoLuongTon -= ct.SoLuong;
                }

                don.TrangThai = trangThai;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["ToastMessage"] = "Đã cập nhật trạng thái đơn.";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ToastMessage"] = "Lỗi khi cập nhật: " + ex.Message;
                TempData["ToastType"] = "danger";
                ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
                return View(don);
            }
        }

        // GET: /Admin/DonHangs/Delete/5
        [HttpGet("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();
            return View(don);
        }

        // POST: /Admin/DonHangs/Delete/5
        [HttpPost("Delete/{id:long}")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();

            if (!string.Equals(don.TrangThai, "Đã hủy", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Chỉ xóa đơn đã Hủy. Vui lòng chuyển trạng thái sang \"Đã hủy\" trước.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _db.DonHangs.Remove(don);
                await _db.SaveChangesAsync();
                TempData["ToastMessage"] = "Đã xóa đơn hàng.";
                TempData["ToastType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = "Không thể xóa: " + ex.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
