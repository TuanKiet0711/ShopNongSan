using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ShopNongSan.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _cfg;
        private readonly IHttpContextAccessor _http;

        public VnPayService(IOptions<VnPaySettings> opt, IHttpContextAccessor http)
        {
            _cfg = opt.Value;
            _http = http;
        }

        private string ClientIp() => "127.0.0.1"; // sandbox

        public string CreatePaymentUrl(string orderCode, decimal amountVnd, string? orderInfo = null, string? bankCode = null, string locale = "vn")
        {
            var info = (orderInfo ?? $"Thanh toan don hang {orderCode}").Trim();
            if (info.Length > 240) info = info[..240];

            var dict = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _cfg.TmnCode,
                ["vnp_Amount"] = ((long)(amountVnd * 100)).ToString(),
                ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ClientIp(),
                ["vnp_Locale"] = locale,
                ["vnp_OrderInfo"] = info,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = _cfg.ReturnUrl,
                ["vnp_TxnRef"] = orderCode
            };
            if (!string.IsNullOrWhiteSpace(bankCode)) dict["vnp_BankCode"] = bankCode;

            var queryNoHash = VnPayHelper.BuildQuery(dict);
            var secureHash = VnPayHelper.HmacSHA512(_cfg.HashSecret, queryNoHash);
            var fullQuery = $"{queryNoHash}&vnp_SecureHashType=HMACSHA512&vnp_SecureHash={secureHash}";
            return $"{_cfg.BaseUrl}?{fullQuery}";
        }

        public bool ValidateReturn(IQueryCollection query, out string orderCode, out string responseCode, out long amount)
        {
            orderCode = query["vnp_TxnRef"];
            responseCode = query["vnp_ResponseCode"];
            long.TryParse(query["vnp_Amount"], out amount);

            var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in query)
            {
                if (!kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)) continue;
                data[kv.Key] = kv.Value.ToString();
            }

            var raw = VnPayHelper.BuildQuery(data);
            var check = VnPayHelper.HmacSHA512(_cfg.HashSecret, raw);
            var received = query["vnp_SecureHash"].ToString();
            return string.Equals(check, received, StringComparison.OrdinalIgnoreCase);
        }
    }
}
