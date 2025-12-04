using Microsoft.AspNetCore.Mvc;
using Stripe;
using ShopNongSan.Models;   // nhớ using namespace DbContext
namespace ShopNongSan.Controllers
{


    public class PaymentController : Controller
    {
        private readonly IConfiguration _config;
        private readonly NongSanContext _db;

        public PaymentController(IConfiguration config, NongSanContext db)
        {
            _config = config;
            _db = db;
        }


        // ⭐ Tạo PaymentIntent
        [HttpPost("/payment/create-intent")]
        public IActionResult CreateIntent([FromBody] CreateIntentRequest req)
        {
            try
            {
                // Debug xem dữ liệu có về đúng không
                Console.WriteLine(">>> RAW vndAmount = " + req.vndAmount);

                // ⭐ Chuyển string → long (loại bỏ dấu phẩy, dấu chấm)
                long vnd = long.Parse(
                    req.vndAmount
                    .Replace(".", "")
                    .Replace(",", "")
                    .Trim()
                );

                Console.WriteLine(">>> VND cleaned = " + vnd);

                // ⭐ Convert VND → USD
                decimal usd = Math.Round(vnd / 25000m, 2);
                long amountInCents = (long)(usd * 100);

                Console.WriteLine(">>> USD = " + usd + ", cents = " + amountInCents);

                StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,   // phải tính bằng CENT!!!
                    Currency = "usd",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new PaymentIntentService();
                var intent = service.Create(options);

                return Json(new { clientSecret = intent.ClientSecret });
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Stripe ERROR: " + ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        // ⭐ Trang kết quả
        [HttpGet("/payment/success")]
        public async Task<IActionResult> Success(long orderId)
        {
            var order = await _db.DonHangs.FindAsync(orderId);
            if (order != null)
            {
                if (string.Equals(order.TrangThai, "Chờ xử lý", StringComparison.OrdinalIgnoreCase))
                {
                    order.TrangThai = "Đã xác nhận";
                    await _db.SaveChangesAsync();
                }

                TempData["toast"] = $"Thanh toán Stripe thành công cho đơn {order.MaDonHang}.";
                TempData["toastType"] = "success";

                // 👉 quay về trang chi tiết đơn (view bạn gửi)
                return Redirect($"/Customer/DonHangs/Details/{order.Id}");
            }

            TempData["toast"] = "Không tìm thấy đơn hàng sau khi thanh toán.";
            TempData["toastType"] = "danger";
            return Redirect("/Customer/DonHangs");
        }


        [HttpGet("/payment/fail")]
        public IActionResult Fail() => View();

        // ⭐ Trang giao diện Payment Element
        [HttpGet("/payment/pay")]
        public IActionResult Pay(decimal amount, long orderId)
        {
            ViewBag.Amount = amount;
            ViewBag.OrderId = orderId;  // ⭐ truyền sang view
            ViewBag.StripePk = _config["Stripe:PublishableKey"];
            return View();
        }


    }

    // ⭐ Model nhận từ fetch (JSON body)
    public class CreateIntentRequest
    {
        public string vndAmount { get; set; }
    }
}
