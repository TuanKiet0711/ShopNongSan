using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShopNongSan.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly NongSanContext _context;

        public HomeController(NongSanContext context)
        {
            _context = context;
        }

        // ================= DASHBOARD =================
        [HttpGet]
        public IActionResult Index(DateTime? from, DateTime? to)
        {
            /* ===== THẺ TỔNG QUAN ===== */
            ViewBag.TotalProducts = _context.SanPhams.Count();
            ViewBag.TotalCategories = _context.DanhMucs.Count();
            ViewBag.TotalOrders = _context.DonHangs.Count();
            ViewBag.TotalBrands = _context.ThuongHieus.Count();

            ViewBag.TotalRevenue = _context.DonHangs
                .Where(d => d.TrangThai != "Cancelled" && d.TrangThai != "Đã hủy")
                .Sum(d => (decimal?)d.TongTien) ?? 0m;

            /* ===== KHOẢNG NGÀY ===== */
            var today = DateTime.Today;
            var fromDate = from?.Date ?? today.AddDays(-6);
            var toDate = to?.Date ?? today;

            if (fromDate > toDate)
            {
                var tmp = fromDate;
                fromDate = toDate;
                toDate = tmp;
            }

            /* ===== LẤY DỮ LIỆU ===== */
            var raw = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.NgayDat.HasValue
                         && d.NgayDat.Value.Date >= fromDate
                         && d.NgayDat.Value.Date <= toDate
                         && d.TrangThai != "Cancelled"
                         && d.TrangThai != "Đã hủy")
                .Select(d => new
                {
                    Ngay = d.NgayDat!.Value.Date,
                    d.TongTien
                })
                .ToList()
                .GroupBy(x => x.Ngay)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalRevenue = g.Sum(x => x.TongTien),
                    TotalOrders = g.Count()
                });

            /* ===== LẤP NGÀY TRỐNG ===== */
            int days = (toDate - fromDate).Days;
            var labels = new List<string>();
            var revenueValues = new List<decimal>();
            var orderValues = new List<int>();

            for (int i = 0; i <= days; i++)
            {
                var d = fromDate.AddDays(i);
                labels.Add(d.ToString("dd/MM"));

                if (raw.TryGetValue(d, out var v))
                {
                    revenueValues.Add(v.TotalRevenue);
                    orderValues.Add(v.TotalOrders);
                }
                else
                {
                    revenueValues.Add(0);
                    orderValues.Add(0);
                }
            }

            /* ===== VIEWBAG ===== */
            ViewBag.FilterFrom = fromDate.ToString("yyyy-MM-dd");
            ViewBag.FilterTo = toDate.ToString("yyyy-MM-dd");
            ViewBag.FilterFromText = fromDate.ToString("dd/MM/yyyy");
            ViewBag.FilterToText = toDate.ToString("dd/MM/yyyy");

            ViewBag.RevenueLabels = labels.ToArray();
            ViewBag.RevenueValues = revenueValues.ToArray();
            ViewBag.OrderValues = orderValues.ToArray();

            ViewBag.RangeTotalRevenue = revenueValues.Sum();
            ViewBag.RangeTotalOrders = orderValues.Sum();

            return View();
        }

        // ================= EXPORT EXCEL =================
        [HttpGet]
        public IActionResult ExportReport(DateTime? from, DateTime? to)
        {
            var today = DateTime.Today;
            var fromDate = from?.Date ?? today.AddDays(-6);
            var toDate = to?.Date ?? today;

            if (fromDate > toDate)
            {
                var tmp = fromDate;
                fromDate = toDate;
                toDate = tmp;
            }

            var data = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.NgayDat.HasValue
                         && d.NgayDat.Value.Date >= fromDate
                         && d.NgayDat.Value.Date <= toDate)
                .GroupBy(d => d.NgayDat!.Value.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    DonDaDat = g.Count(d => d.TrangThai != "Cancelled" && d.TrangThai != "Đã hủy"),
                    DonBiHuy = g.Count(d => d.TrangThai == "Cancelled" || d.TrangThai == "Đã hủy"),
                    DoanhThu = g
                        .Where(d => d.TrangThai != "Cancelled" && d.TrangThai != "Đã hủy")
                        .Sum(d => (decimal?)d.TongTien) ?? 0
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("BaoCaoDonHang");

            // ===== HEADER =====
            ws.Cells[1, 1].Value = "BÁO CÁO ĐƠN HÀNG";
            ws.Cells[1, 1, 1, 4].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[2, 1].Value = $"Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}";
            ws.Cells[2, 1, 2, 4].Merge = true;

            // ===== TABLE HEADER =====
            ws.Cells[4, 1].Value = "Ngày";
            ws.Cells[4, 2].Value = "Đơn đã đặt";
            ws.Cells[4, 3].Value = "Đơn bị hủy";
            ws.Cells[4, 4].Value = "Doanh thu (₫)";
            ws.Cells[4, 1, 4, 4].Style.Font.Bold = true;

            // ===== DATA =====
            int row = 5;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.Ngay.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = item.DonDaDat;
                ws.Cells[row, 3].Value = item.DonBiHuy;
                ws.Cells[row, 4].Value = item.DoanhThu;
                row++;
            }

            ws.Cells[5, 4, row, 4].Style.Numberformat.Format = "#,##0 ₫";
            ws.Cells.AutoFitColumns();

            var fileName = $"BaoCao_DonHang_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
