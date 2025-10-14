using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
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

        // Dashboard + Báo cáo theo khoảng ngày (dùng chung Index)
        [HttpGet]
        public IActionResult Index(DateTime? from, DateTime? to)
        {
            // Thẻ tổng quan
            ViewBag.TotalProducts = _context.SanPhams.Count();
            ViewBag.TotalCategories = _context.DanhMucs.Count();
            ViewBag.TotalOrders = _context.DonHangs.Count();
            ViewBag.TotalBrands = _context.ThuongHieus.Count(); // <- thêm thẻ Thương hiệu

            // An toàn nếu TongTien có null
            ViewBag.TotalRevenue = _context.DonHangs.Sum(d => (decimal?)d.TongTien) ?? 0m;

            // Khoảng ngày cho biểu đồ (mặc định 7 ngày gần nhất)
            var today = DateTime.Today;
            var fromDate = (from?.Date) ?? today.AddDays(-6);
            var toDate = (to?.Date) ?? today;
            if (fromDate > toDate) { var tmp = fromDate; fromDate = toDate; toDate = tmp; }

            // Lấy dữ liệu đơn trong khoảng (loại hủy) và group theo ngày (group trên bộ nhớ cho chắc)
            var raw = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.NgayDat.HasValue
                         && d.NgayDat.Value.Date >= fromDate
                         && d.NgayDat.Value.Date <= toDate
                         && d.TrangThai != "Cancelled"
                         && d.TrangThai != "Đã hủy")
                .Select(d => new { d.NgayDat, d.TongTien })
                .ToList()
                .GroupBy(x => x.NgayDat!.Value.Date)
                .Select(g => new { Date = g.Key, TotalRevenue = g.Sum(x => x.TongTien), TotalOrders = g.Count() })
                .ToDictionary(k => k.Date, v => v);

            // Lấp đủ các ngày trống
            int days = (toDate - fromDate).Days;
            var labels = new List<string>(days + 1);
            var revenueValues = new List<decimal>(days + 1);
            var orderValues = new List<int>(days + 1);

            for (int i = 0; i <= days; i++)
            {
                var d = fromDate.AddDays(i);
                if (raw.TryGetValue(d, out var v))
                {
                    labels.Add(d.ToString("dd/MM"));
                    revenueValues.Add(v.TotalRevenue);
                    orderValues.Add(v.TotalOrders);
                }
                else
                {
                    labels.Add(d.ToString("dd/MM"));
                    revenueValues.Add(0);
                    orderValues.Add(0);
                }
            }

            // Xuất ViewBag
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
    }
}
