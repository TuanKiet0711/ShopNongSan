using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class DonHangsController : Controller
    {
        private readonly NongSanContext _db;
        public DonHangsController(NongSanContext db) => _db = db;

        // Khớp với CHECK constraint trong DB
        private static readonly string[] AllowedStatuses =
            new[] { "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" };

        // GET: /Admin/DonHangs
        public async Task<IActionResult> Index(string? status, string? q)
        {
            var query = _db.DonHangs
                .Include(d => d.TaiKhoan)
                .OrderByDescending(d => d.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.TrangThai == status);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(d => d.MaDonHang.Contains(q));

            ViewBag.StatusList = new SelectList(AllowedStatuses);
            ViewBag.FilterStatus = status;
            ViewBag.Q = q;

            var list = await query.Take(500).ToListAsync();
            return View(list);
        }

        // GET: /Admin/DonHangs/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung) // 👈 thêm dòng này
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (don == null) return NotFound();
            return View(don);
        }

        // GET: /Admin/DonHangs/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung) // 👈 thêm dòng này
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();

            ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
            return View(don);
        }

        // POST: /Admin/DonHangs/Edit/5
        [HttpPost]
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
                // Đổi sang Cancelled (từ trạng thái khác) → hoàn kho
                if (trangThai == "Cancelled" &&
                    !string.Equals(don.TrangThai, "Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ct in don.DonHangChiTiets)
                    {
                        if (ct.SanPham != null)
                            ct.SanPham.SoLuongTon += ct.SoLuong;
                    }
                }

                // Bỏ Cancelled (Cancel → khác) → trừ kho lại (nếu đủ)
                if (don.TrangThai == "Cancelled" && trangThai != "Cancelled")
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
                    {
                        if (ct.SanPham != null)
                            ct.SanPham.SoLuongTon -= ct.SoLuong;
                    }
                }

                don.TrangThai = trangThai;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["toast"] = "Đã cập nhật trạng thái đơn.";
                TempData["toastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["toast"] = "Lỗi khi cập nhật: " + ex.Message;
                TempData["toastType"] = "danger";
                ViewBag.StatusList = new SelectList(AllowedStatuses, don.TrangThai);
                return View(don);
            }
        }

        // GET: /Admin/DonHangs/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.TaiKhoan)
                    .ThenInclude(tk => tk.ThongTinNguoiDung) // 👈 thêm dòng này
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();
            return View(don);
        }

        // POST: /Admin/DonHangs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var don = await _db.DonHangs
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (don == null) return NotFound();

            if (!string.Equals(don.TrangThai, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                TempData["toast"] = "Chỉ xóa đơn đã Hủy. Vui lòng chuyển trạng thái sang Cancelled trước.";
                TempData["toastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _db.DonHangs.Remove(don);
                await _db.SaveChangesAsync();
                TempData["toast"] = "Đã xóa đơn hàng.";
                TempData["toastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["toast"] = "Không thể xóa: " + ex.Message;
                TempData["toastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
