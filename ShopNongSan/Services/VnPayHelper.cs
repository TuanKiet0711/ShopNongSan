using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ShopNongSan.Services
{
    public static class VnPayHelper
    {
        private static string ToHexUpper(byte[] data)
            => BitConverter.ToString(data).Replace("-", "");

        public static string HmacSHA512(string key, string rawData)
        {
            using var h = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return ToHexUpper(h.ComputeHash(Encoding.UTF8.GetBytes(rawData)));
        }

        // UrlEncode với space -> '+', sort theo key
        public static string BuildQuery(IDictionary<string, string> dict)
        {
            var ordered = dict.Where(kv => !string.IsNullOrEmpty(kv.Value))
                              .OrderBy(kv => kv.Key, StringComparer.Ordinal);
            var sb = new StringBuilder();
            foreach (var p in ordered)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(p.Key).Append('=').Append(WebUtility.UrlEncode(p.Value));
            }
            return sb.ToString();
        }
    }
}
