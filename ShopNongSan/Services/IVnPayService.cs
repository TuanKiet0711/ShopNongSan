namespace ShopNongSan.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(string orderCode, decimal amountVnd, string? orderInfo = null, string? bankCode = null, string locale = "vn");
        bool ValidateReturn(IQueryCollection query, out string orderCode, out string responseCode, out long amount);
    }
}
